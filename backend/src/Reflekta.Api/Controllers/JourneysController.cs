using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;
using Reflekta.Api.Services;

namespace Reflekta.Api.Controllers;

public record CreateJourneyRequest(Guid IntentionId);
public record RollResultDto(int DiceResult, Guid CardId, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, int SequenceNumber);
public record SubmitReflectionRequest(string Text);
public record ReflectionDto(Guid Id, Guid PlayedCardId, string Text, DateTimeOffset CreatedAt);
public record JourneyDto(Guid Id, Guid IntentionId, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);
public record ConversationMessageDto(Guid Id, string Role, string Content, DateTimeOffset CreatedAt)
{
    public static ConversationMessageDto From(ConversationMessage m) => new(m.Id, m.Role, m.Content, m.CreatedAt);
}

public record JourneyDetailDto(Guid Id, string IntentionText, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, List<PlayedCardDetailDto> PlayedCards, SessionSummaryDto? Summary);

public record ReflectionWithConversationDto(Guid Id, Guid PlayedCardId, string Text, DateTimeOffset CreatedAt, List<ConversationMessageDto> Messages, bool AiUnavailable);
public record CurrentCardDto(int DiceResult, Guid CardId, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, int SequenceNumber, string? ReflectionText, List<ConversationMessageDto> Messages);

public record SendMessageRequest(string Content);
public record SendMessageResponse(ConversationMessageDto UserMessage, ConversationMessageDto AssistantMessage);
public record ReplyResponse(ConversationMessageDto AssistantMessage);
public record JourneySummaryDto(Guid Id, string IntentionText, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CardCount);
public record PlayedCardDetailDto(Guid Id, int SequenceNumber, int DiceResult, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, string? ReflectionText);

public record SessionSummaryDto(Guid Id, Guid JourneyId, string SummaryText, List<string> Themes, DateTimeOffset CreatedAt)
{
    public static SessionSummaryDto From(SessionSummary s) => new(s.Id, s.JourneyId, s.SummaryText, s.Themes, s.CreatedAt);
}


[ApiController]
[Route("api/journeys")]
[Authorize]
public class JourneysController : ControllerBase
{
    
    private readonly ReflektaDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDiceService _diceService;
    private readonly ICardSelectionService _cardSelectionService;
    private readonly ConversationTurnService _conversationTurnService;
    private readonly SessionSummaryService _sessionSummaryService;



    public JourneysController(
        ReflektaDbContext dbContext,
        ICurrentUserService currentUserService,
        IDiceService diceService,
        ICardSelectionService cardSelectionService,
        ConversationTurnService conversationTurnService,
        SessionSummaryService sessionSummaryService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _diceService = diceService;
        _cardSelectionService = cardSelectionService;
        _conversationTurnService = conversationTurnService;
        _sessionSummaryService = sessionSummaryService;
    }


    [HttpPost]
    public async Task<ActionResult<JourneyDto>> Create([FromBody] CreateJourneyRequest request, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var intention = await _dbContext.Intentions.FirstOrDefaultAsync(i => i.Id == request.IntentionId, ct);
        if (intention is null)
            return NotFound("Intention not found.");
        if (intention.UserId != userId)
            return Forbid();

        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IntentionId = intention.Id,
            Status = JourneyStatus.Active,
            StartedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Journeys.Add(journey);
        await _dbContext.SaveChangesAsync(ct);
        
        return Ok(new JourneyDto(journey.Id, journey.IntentionId, journey.Status.ToString(), journey.StartedAt, journey.CompletedAt));

    }

    [HttpPost("{journeyId:guid}/roll")]
    public async Task<ActionResult<RollResultDto>> Roll(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();
        if (journey.Status != JourneyStatus.Active)
            return BadRequest("Journey is not active.");

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed is not null)
        {
            var hasReflection = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayed.Id, ct);
            if (!hasReflection)
                return BadRequest("Write a reflection before continuing the journey.");
        }

        var cards = await _dbContext.Cards.AsNoTracking().ToListAsync(ct);

        var currentPosition = lastPlayed is null
            ? 0
            : cards.Single(c => c.Id == lastPlayed.CardId).BoardPosition;

        var diceResult = _diceService.Roll();
        var selection = _cardSelectionService.SelectNext(currentPosition, diceResult, cards);

        var playedCard = new PlayedCard
        {
            Id = Guid.NewGuid(),
            JourneyId = journey.Id,
            CardId = selection.Card.Id,
            SequenceNumber = journey.PlayedCards.Count + 1,
            DiceResult = diceResult,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.PlayedCards.Add(playedCard);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new RollResultDto(
            diceResult, selection.Card.Id, selection.Card.Title, selection.Card.WisdomText,
            selection.Card.ReflectionPrompt, selection.Card.Themes, playedCard.SequenceNumber));
    }

    [HttpGet("{journeyId:guid}/current-card")]
    public async Task<ActionResult<RollResultDto>> GetCurrentCard(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards).ThenInclude(pc => pc.Card)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed?.Card is null)
            return NotFound("No card has been played yet.");

        var reflectionText = await _dbContext.Reflections
            .Where(r => r.PlayedCardId == lastPlayed.Id)
            .Select(r => r.Text)
            .FirstOrDefaultAsync(ct);

        var messages = await _dbContext.ConversationMessages
            .Where(m => m.PlayedCardId == lastPlayed.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return Ok(new CurrentCardDto(
            lastPlayed.DiceResult, lastPlayed.Card.Id, lastPlayed.Card.Title, lastPlayed.Card.WisdomText,
            lastPlayed.Card.ReflectionPrompt, lastPlayed.Card.Themes, lastPlayed.SequenceNumber,
            reflectionText, messages.Select(ConversationMessageDto.From).ToList()));

    }

    
    [HttpPost("{journeyId:guid}/reflection")]
    public async Task<ActionResult<ReflectionWithConversationDto>> SubmitReflection(Guid journeyId, [FromBody] SubmitReflectionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Reflection text is required.");

        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed is null)
            return BadRequest("Roll before writing a reflection.");

        var alreadyReflected = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayed.Id, ct);
        if (alreadyReflected)
            return BadRequest("This card already has a reflection.");

        var reflection = new Reflection
        {
            Id = Guid.NewGuid(),
            PlayedCardId = lastPlayed.Id,
            Text = request.Text.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Reflections.Add(reflection);
        await _dbContext.SaveChangesAsync(ct);

        var messages = new List<ConversationMessageDto>();
        var aiUnavailable = false;
        try
        {
            var assistantMessage = await _conversationTurnService.ReplyAsync(journey, lastPlayed, ct);
            messages.Add(ConversationMessageDto.From(assistantMessage));
        }
        catch (AiUnavailableException)
        {
            aiUnavailable = true;
        }

        return Ok(new ReflectionWithConversationDto(
            reflection.Id, reflection.PlayedCardId, reflection.Text, reflection.CreatedAt, messages, aiUnavailable));

    }

    [HttpPost("{journeyId:guid}/complete")]
    public async Task<ActionResult<JourneyDto>> Complete(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();
        if (journey.Status != JourneyStatus.Active)
            return BadRequest("Journey is already completed.");

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed is null)
            return BadRequest("Roll at least once before completing the journey.");

        var hasReflection = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayed.Id, ct);
        if (!hasReflection)
            return BadRequest("Write a reflection before completing the journey.");

        journey.Status = JourneyStatus.Completed;
        journey.CompletedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        try
        {
            await _sessionSummaryService.GenerateAsync(journey, ct);
        }
        catch (AiUnavailableException)
        {
            // The journey still completes; the user can retry generating the summary later.
        }


        return Ok(new JourneyDto(journey.Id, journey.IntentionId, journey.Status.ToString(), journey.StartedAt, journey.CompletedAt));
    }
    
    [HttpGet("{journeyId:guid}/summary")]
    public async Task<ActionResult<SessionSummaryDto>> GetSummary(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys.FirstOrDefaultAsync(j => j.Id == journeyId, ct);
        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();

        var summary = await _dbContext.SessionSummaries.FirstOrDefaultAsync(s => s.JourneyId == journeyId, ct);
        if (summary is null)
            return NotFound();

        return Ok(SessionSummaryDto.From(summary));
    }

    [HttpPost("{journeyId:guid}/summary/retry")]
    public async Task<ActionResult<SessionSummaryDto>> RetrySummary(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys.FirstOrDefaultAsync(j => j.Id == journeyId, ct);
        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();
        if (journey.Status != JourneyStatus.Completed)
            return BadRequest("Complete the journey before generating a summary.");

        var alreadyExists = await _dbContext.SessionSummaries.AnyAsync(s => s.JourneyId == journeyId, ct);
        if (alreadyExists)
            return BadRequest("This journey already has a summary.");

        try
        {
            var summary = await _sessionSummaryService.GenerateAsync(journey, ct);
            return Ok(SessionSummaryDto.From(summary));
        }
        catch (AiUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The AI facilitator is unavailable.");
        }
    }



    [HttpGet]
    public async Task<ActionResult<List<JourneySummaryDto>>> List(CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journeys = await _dbContext.Journeys
            .Include(j => j.Intention)
            .Include(j => j.PlayedCards)
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync(ct);

        var result = journeys
            .Select(j => new JourneySummaryDto(
                j.Id, j.Intention!.OriginalText, j.Status.ToString(), j.StartedAt, j.CompletedAt, j.PlayedCards.Count))
            .ToList();

        return Ok(result);
    }

    [HttpGet("{journeyId:guid}")]
    public async Task<ActionResult<JourneyDetailDto>> GetById(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.Intention)
            .Include(j => j.PlayedCards).ThenInclude(pc => pc.Card)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();

        var playedCardIds = journey.PlayedCards.Select(pc => pc.Id).ToList();
        var reflectionsByPlayedCardId = await _dbContext.Reflections
            .Where(r => playedCardIds.Contains(r.PlayedCardId))
            .ToDictionaryAsync(r => r.PlayedCardId, r => r.Text, ct);

        var playedCards = journey.PlayedCards
            .OrderBy(pc => pc.SequenceNumber)
            .Select(pc => new PlayedCardDetailDto(
                pc.Id, pc.SequenceNumber, pc.DiceResult, pc.Card!.Title, pc.Card.WisdomText,
                pc.Card.ReflectionPrompt, pc.Card.Themes,
                reflectionsByPlayedCardId.GetValueOrDefault(pc.Id)))
            .ToList();

        var summary = await _dbContext.SessionSummaries.FirstOrDefaultAsync(s => s.JourneyId == journeyId, ct);

        return Ok(new JourneyDetailDto(
            journey.Id, journey.Intention!.OriginalText, journey.Status.ToString(),
            journey.StartedAt, journey.CompletedAt, playedCards,
            summary is null ? null : SessionSummaryDto.From(summary)));
    }

    private const int MaxMessageLength = 2000;

    /// <summary>
    /// Loads the journey and its latest played card for a conversation action, or returns the
    /// error result (404/403/400) that the caller should send back.
    /// </summary>
    private async Task<(Journey? Journey, PlayedCard? PlayedCard, ActionResult? Error)> LoadConversationTargetAsync(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return (null, null, NotFound());
        if (journey.UserId != userId)
            return (null, null, Forbid());
        if (journey.Status != JourneyStatus.Active)
            return (null, null, BadRequest("Journey is not active."));

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed is null)
            return (null, null, BadRequest("Roll before starting a conversation."));

        var hasReflection = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayed.Id, ct);
        if (!hasReflection)
            return (null, null, BadRequest("Write a reflection before starting a conversation."));

        return (journey, lastPlayed, null);
    }

    [HttpPost("{journeyId:guid}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(Guid journeyId, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Message content is required.");
        if (request.Content.Length > MaxMessageLength)
            return BadRequest($"Message must be at most {MaxMessageLength} characters.");

        var (journey, playedCard, error) = await LoadConversationTargetAsync(journeyId, ct);
        if (error is not null)
            return error;

        var userMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            PlayedCardId = playedCard!.Id,
            Role = ConversationRoles.User,
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.ConversationMessages.Add(userMessage);
        await _dbContext.SaveChangesAsync(ct);

        try
        {
            var assistantMessage = await _conversationTurnService.ReplyAsync(journey!, playedCard, ct);
            return Ok(new SendMessageResponse(ConversationMessageDto.From(userMessage), ConversationMessageDto.From(assistantMessage)));
        }
        catch (AiUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The AI facilitator is unavailable. Your message was saved.");
        }
    }

    [HttpPost("{journeyId:guid}/messages/reply")]
    public async Task<ActionResult<ReplyResponse>> RequestReply(Guid journeyId, CancellationToken ct)
    {
        var (journey, playedCard, error) = await LoadConversationTargetAsync(journeyId, ct);
        if (error is not null)
            return error;

        var lastMessage = await _dbContext.ConversationMessages
            .Where(m => m.PlayedCardId == playedCard!.Id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (lastMessage?.Role == ConversationRoles.Assistant)
            return BadRequest("The latest message has already been answered.");

        try
        {
            var assistantMessage = await _conversationTurnService.ReplyAsync(journey!, playedCard!, ct);
            return Ok(new ReplyResponse(ConversationMessageDto.From(assistantMessage)));
        }
        catch (AiUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The AI facilitator is unavailable.");
        }
    }

    


}