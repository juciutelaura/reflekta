using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;

namespace Reflekta.Api.Tests.Data;

public class ConversationMessagePersistenceTests
{
    [Fact]
    public async Task SavesAndLoadsMessagesForAPlayedCardInOrder()
    {
        var options = new DbContextOptionsBuilder<ReflektaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var playedCardId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow;

        await using (var context = new ReflektaDbContext(options))
        {
            context.ConversationMessages.AddRange(
                new ConversationMessage { Id = Guid.NewGuid(), PlayedCardId = playedCardId, Role = ConversationRoles.User, Content = "Second", CreatedAt = start.AddSeconds(1) },
                new ConversationMessage { Id = Guid.NewGuid(), PlayedCardId = playedCardId, Role = ConversationRoles.Assistant, Content = "First", CreatedAt = start });
            await context.SaveChangesAsync();
        }

        await using (var context = new ReflektaDbContext(options))
        {
            var messages = await context.ConversationMessages
                .Where(m => m.PlayedCardId == playedCardId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            Assert.Equal(["First", "Second"], messages.Select(m => m.Content));
            Assert.Equal(ConversationRoles.Assistant, messages[0].Role);
        }
    }
}
