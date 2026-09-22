using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Models;

namespace Reflekta.Api.Data;

public static class CardSeeder
{
    // Authoritative Phase 1 card content — reproduced exactly from
    // docs/PRODUCT_REQUIREMENTS.md §9 "MVP Card Content". Do not edit, translate,
    // reorder, or add to this list; see that document's "Content rules".
    public static async Task SeedAsync(ReflektaDbContext context, CancellationToken ct = default)
    {
        if (await context.Cards.AnyAsync(ct))
            return;

        var cards = new List<Card>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Stebėtojas",
                Themes = new() { "Sąmoningumas" },
                WisdomText = "Ne kiekviena mintis reikalauja tavo atsakymo. Kartais pirmas žingsnis yra pastebėti, kas vyksta tavo viduje, nebandant to pakeisti.",
                ReflectionPrompt = "Ką pastebi savyje, kai tiesiog stebi savo mintis, jų nevertindamas?",
                BoardPosition = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "„Aš“",
                Themes = new() { "Tapatybė" },
                WisdomText = "Mes dažnai kalbame apie save taip, lyg jau tiksliai žinotume, kas esame. Tačiau dalis to, ką vadiname „aš“, gali būti istorijos, kurias apie save kartojame.",
                ReflectionPrompt = "Kuri istorija apie save tau atrodo tokia pažįstama, kad retai ją kvestionuoji?",
                BoardPosition = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Už durų",
                Themes = new() { "Baimė" },
                WisdomText = "Baimė dažnai kalba apie tai, kas gali nutikti. Tačiau kartais ji daugiau pasako apie tai, ką stengiamės apsaugoti.",
                ReflectionPrompt = "Jeigu pažvelgtum už savo baimės — ką ji galbūt bando apsaugoti?",
                BoardPosition = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Paleidimas",
                Themes = new() { "Kontrolė" },
                WisdomText = "Noras kontroliuoti gali suteikti saugumo jausmą. Tačiau ne viskas, kas vyksta tavo gyvenime, yra tavo rankose.",
                ReflectionPrompt = "Ko šiandien labiausiai stengiesi kontroliuoti?",
                BoardPosition = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Tarp",
                Themes = new() { "Pokytis" },
                WisdomText = "Pokytis ne visada prasideda nuo aiškaus sprendimo. Kartais pirmiausia atsiranda jausmas, kad tai, kas anksčiau tiko, nebetinka, nors dar nežinai, kas bus toliau.",
                ReflectionPrompt = "Kas tavo gyvenime šiuo metu atrodo tarsi „tarp“ — tarp to, kas buvo, ir to, kas dar tik atsiranda?",
                BoardPosition = 4
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Nežinau",
                Themes = new() { "Nežinomybė" },
                WisdomText = "Nežinojimas gali atrodyti kaip problema, kurią reikia kuo greičiau išspręsti. Tačiau kartais atsakymo paieška per anksti neleidžia pamatyti to, kas dar tik formuojasi.",
                ReflectionPrompt = "Kurioje savo gyvenimo vietoje tau sunkiausia pasakyti „aš dar nežinau“?",
                BoardPosition = 5
            },
        };

        context.Cards.AddRange(cards);
        await context.SaveChangesAsync(ct);
    }
}
