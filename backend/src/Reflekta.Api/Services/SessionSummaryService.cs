using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;

namespace Reflekta.Api.Services;

/// <summary>
/// Generates and persists the session summary for a completed journey: loads every played card's
/// reflection and conversation, asks the AI service for a structured summary, and saves it.
/// Throws <see cref="AiUnavailableException"/> without saving anything when the AI cannot reply.
/// </summary>
public class SessionSummaryService(ReflektaDbContext dbContext, IAiClient aiClient)
{
    public async Task<SessionSummary> GenerateAsync(Journey journey, CancellationToken ct)
    {
        var intention = await dbContext.Intentions.AsNoTracking().SingleAsync(i => i.Id == journey.IntentionId, ct);

        var playedCards = await dbContext.PlayedCards.AsNoTracking()
            .Where(pc => pc.JourneyId == journey.Id)
            .Include(pc => pc.Card)
            .OrderBy(pc => pc.SequenceNumber)
            .ToListAsync(ct);

        var playedCardContexts = new List<SummaryPlayedCard>();
        foreach (var playedCard in playedCards)
        {
            var reflection = await dbContext.Reflections.AsNoTracking()
                .SingleAsync(r => r.PlayedCardId == playedCard.Id, ct);
            var messages = await dbContext.ConversationMessages.AsNoTracking()
                .Where(m => m.PlayedCardId == playedCard.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);

            playedCardContexts.Add(new SummaryPlayedCard(
                new SummaryCard(playedCard.Card!.Title, playedCard.Card.WisdomText),
                reflection.Text,
                messages.Select(m => new AiMessage(m.Role, m.Content)).ToList()));
        }

        var context = new SummaryContext(intention.OriginalText, playedCardContexts);
        var result = await aiClient.SummarizeAsync(context, ct);

        var summary = new SessionSummary
        {
            Id = Guid.NewGuid(),
            JourneyId = journey.Id,
            SummaryText = result.SummaryText,
            Themes = result.Themes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.SessionSummaries.Add(summary);
        await dbContext.SaveChangesAsync(ct);

        return summary;
    }
}
