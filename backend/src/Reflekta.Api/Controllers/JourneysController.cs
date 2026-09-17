using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;
using Reflekta.Api.Services;

namespace Reflekta.Api.Controllers;

public record CreateJourneyRequest(Guid IntentionId);
public record JourneyDto(Guid Id, Guid IntentionId, string Status, DateTimeOffset StartedAt);
public record RollResultDto(int DiceResult, Guid CardId, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, int SequenceNumber);

[ApiController]
[Route("api/journeys")]
[Authorize]
public class JourneysController : ControllerBase
{
    private readonly ReflektaDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDiceService _diceService;
    private readonly ICardSelectionService _cardSelectionService;

    public JourneysController(
        ReflektaDbContext dbContext,
        ICurrentUserService currentUserService,
        IDiceService diceService,
        ICardSelectionService cardSelectionService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _diceService = diceService;
        _cardSelectionService = cardSelectionService;
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

        return Ok(new JourneyDto(journey.Id, journey.IntentionId, journey.Status.ToString(), journey.StartedAt));
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

        var cards = await _dbContext.Cards.AsNoTracking().ToListAsync(ct);

        var currentPosition = 0;
        if (journey.PlayedCards.Count > 0)
        {
            var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).First();
            currentPosition = cards.Single(c => c.Id == lastPlayed.CardId).BoardPosition;
        }

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

        return Ok(new RollResultDto(
            lastPlayed.DiceResult, lastPlayed.Card.Id, lastPlayed.Card.Title, lastPlayed.Card.WisdomText,
            lastPlayed.Card.ReflectionPrompt, lastPlayed.Card.Themes, lastPlayed.SequenceNumber));
    }
}