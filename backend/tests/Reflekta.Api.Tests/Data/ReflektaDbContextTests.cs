using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;
using Xunit;

namespace Reflekta.Api.Tests.Data;

public class ReflektaDbContextTests
{
    [Fact]
    public async Task SavesAndLoadsCardWithThemesReflectionPromptAndBoardPosition()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ReflektaDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var cardId = Guid.NewGuid();
        using (var writeContext = new ReflektaDbContext(options))
        {
            writeContext.Cards.Add(new Card
            {
                Id = cardId,
                Title = "Control",
                WisdomText = "Notice where you try to hold on tightly.",
                ReflectionPrompt = "What are you trying hardest to control today?",
                Themes = new List<string> { "Control", "Fear" },
                BoardPosition = 3
            });
            await writeContext.SaveChangesAsync();
        }

        using var readContext = new ReflektaDbContext(options);
        var loaded = await readContext.Cards.SingleAsync(c => c.Id == cardId);

        Assert.Equal("Control", loaded.Title);
        Assert.Equal("What are you trying hardest to control today?", loaded.ReflectionPrompt);
        Assert.Equal(new List<string> { "Control", "Fear" }, loaded.Themes);
        Assert.Equal(3, loaded.BoardPosition);
    }
}