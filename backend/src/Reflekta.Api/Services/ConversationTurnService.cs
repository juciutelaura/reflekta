using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;

namespace Reflekta.Api.Services;

/// <summary>
/// Produces one assistant message for a played card: loads the context owned by this API,
/// asks the AI service for a reply, and persists it. Throws <see cref="AiUnavailableException"/>
/// without saving anything when the AI cannot reply.
/// </summary>
public class ConversationTurnService(ReflektaDbContext dbContext, IAiClient aiClient)
{
    public async Task<ConversationMessage> ReplyAsync(Journey journey, PlayedCard playedCard, CancellationToken ct)
    {
        var intention = await dbContext.Intentions.AsNoTracking().SingleAsync(i => i.Id == journey.IntentionId, ct);
        var card = await dbContext.Cards.AsNoTracking().SingleAsync(c => c.Id == playedCard.CardId, ct);
        var reflection = await dbContext.Reflections.AsNoTracking().SingleAsync(r => r.PlayedCardId == playedCard.Id, ct);
        var messages = await dbContext.ConversationMessages.AsNoTracking()
            .Where(m => m.PlayedCardId == playedCard.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        var context = new ReflectionContext(
            intention.OriginalText,
            new AiCard(card.Title, card.WisdomText, card.ReflectionPrompt),
            reflection.Text,
            messages.Select(m => new AiMessage(m.Role, m.Content)).ToList());

        var reply = await aiClient.RespondAsync(context, ct);

        var assistantMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            PlayedCardId = playedCard.Id,
            Role = ConversationRoles.Assistant,
            Content = reply,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ConversationMessages.Add(assistantMessage);
        await dbContext.SaveChangesAsync(ct);

        return assistantMessage;
    }
}