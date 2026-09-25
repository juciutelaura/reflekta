using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;
using Xunit;

namespace Reflekta.Api.Tests.Data;

public class ReflectionPersistenceTests
{
    [Fact]
    public async Task SavesAndLoadsAReflectionForAPlayedCard()
    {
        var options = new DbContextOptionsBuilder<ReflektaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var playedCardId = Guid.NewGuid();
        var reflectionId = Guid.NewGuid();

        using (var writeContext = new ReflektaDbContext(options))
        {
            writeContext.Reflections.Add(new Reflection
            {
                Id = reflectionId,
                PlayedCardId = playedCardId,
                Text = "I noticed I get anxious whenever I imagine actually leaving my job.",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await writeContext.SaveChangesAsync();
        }

        using var readContext = new ReflektaDbContext(options);
        var loaded = await readContext.Reflections.SingleAsync(r => r.Id == reflectionId);

        Assert.Equal(playedCardId, loaded.PlayedCardId);
        Assert.Equal("I noticed I get anxious whenever I imagine actually leaving my job.", loaded.Text);
    }
}
