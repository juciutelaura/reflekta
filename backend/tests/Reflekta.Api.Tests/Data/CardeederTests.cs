using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;
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
    public async Task SeedAsync_CreatesSixCardsWithDistinctBoardPositions()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        await CardSeeder.SeedAsync(context);

        var cards = await context.Cards.ToListAsync();
        Assert.Equal(6, cards.Count);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, cards.Select(c => c.BoardPosition).OrderBy(p => p));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        await CardSeeder.SeedAsync(context);
        await CardSeeder.SeedAsync(context);

        Assert.Equal(6, await context.Cards.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ReproducesTheApprovedCardContentExactly()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        await CardSeeder.SeedAsync(context);

        var cards = await context.Cards.OrderBy(c => c.BoardPosition).ToListAsync();

        AssertCard(cards[0], "Stebėtojas", "Sąmoningumas",
            "Ne kiekviena mintis reikalauja tavo atsakymo. Kartais pirmas žingsnis yra pastebėti, kas vyksta tavo viduje, nebandant to pakeisti.",
            "Ką pastebi savyje, kai tiesiog stebi savo mintis, jų nevertindamas?");

        AssertCard(cards[1], "„Aš“", "Tapatybė",
            "Mes dažnai kalbame apie save taip, lyg jau tiksliai žinotume, kas esame. Tačiau dalis to, ką vadiname „aš“, gali būti istorijos, kurias apie save kartojame.",
            "Kuri istorija apie save tau atrodo tokia pažįstama, kad retai ją kvestionuoji?");

        AssertCard(cards[2], "Už durų", "Baimė",
            "Baimė dažnai kalba apie tai, kas gali nutikti. Tačiau kartais ji daugiau pasako apie tai, ką stengiamės apsaugoti.",
            "Jeigu pažvelgtum už savo baimės — ką ji galbūt bando apsaugoti?");

        AssertCard(cards[3], "Paleidimas", "Kontrolė",
            "Noras kontroliuoti gali suteikti saugumo jausmą. Tačiau ne viskas, kas vyksta tavo gyvenime, yra tavo rankose.",
            "Ko šiandien labiausiai stengiesi kontroliuoti?");

        AssertCard(cards[4], "Tarp", "Pokytis",
            "Pokytis ne visada prasideda nuo aiškaus sprendimo. Kartais pirmiausia atsiranda jausmas, kad tai, kas anksčiau tiko, nebetinka, nors dar nežinai, kas bus toliau.",
            "Kas tavo gyvenime šiuo metu atrodo tarsi „tarp“ — tarp to, kas buvo, ir to, kas dar tik atsiranda?");

        AssertCard(cards[5], "Nežinau", "Nežinomybė",
            "Nežinojimas gali atrodyti kaip problema, kurią reikia kuo greičiau išspręsti. Tačiau kartais atsakymo paieška per anksti neleidžia pamatyti to, kas dar tik formuojasi.",
            "Kurioje savo gyvenimo vietoje tau sunkiausia pasakyti „aš dar nežinau“?");
    }

    private static void AssertCard(Card card, string title, string theme, string wisdomText, string reflectionPrompt)
    {
        Assert.Equal(title, card.Title);
        Assert.Equal(new List<string> { theme }, card.Themes);
        Assert.Equal(wisdomText, card.WisdomText);
        Assert.Equal(reflectionPrompt, card.ReflectionPrompt);
    }
}
