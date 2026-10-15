using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Xunit;

namespace Reflekta.Api.Tests.Data;

public class CardSeederTests
{
    private static ReflektaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ReflektaDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ReflektaDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_CreatesSeventyTwoCardsWithDistinctBoardPositions()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        await CardSeeder.SeedAsync(context);

        var cards = await context.Cards.ToListAsync();
        Assert.Equal(72, cards.Count);
        Assert.Equal(Enumerable.Range(0, 72), cards.Select(c => c.BoardPosition).OrderBy(p => p));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        await CardSeeder.SeedAsync(context);
        await CardSeeder.SeedAsync(context);

        Assert.Equal(72, await context.Cards.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ReadsTheFirstCardFromTheDeckFile()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        await CardSeeder.SeedAsync(context);

        var first = await context.Cards.SingleAsync(c => c.BoardPosition == 0);

        Assert.Equal("Pradžia", first.Title);
        Assert.Equal(["Pradžia"], first.Themes);
        Assert.Equal("Kiekviena pradžia dar neturi galutinės formos.", first.WisdomText);
        Assert.Equal(
            "Kas tavo gyvenime šiuo metu galbūt prasideda, net jei dar nežinai, kuo tai taps?",
            first.ReflectionPrompt);
    }
}
