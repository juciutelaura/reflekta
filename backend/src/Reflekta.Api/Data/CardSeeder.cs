using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Models;

namespace Reflekta.Api.Data;

public static class CardSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    // Card content lives in reflekta_72_cards.json, embedded next to this file.
    public static async Task SeedAsync(ReflektaDbContext context, CancellationToken ct = default)
    {
        if (await context.Cards.AnyAsync(ct))
            return;

        context.Cards.AddRange(await LoadCardsAsync(ct));
        await context.SaveChangesAsync(ct);
    }

    private static async Task<List<Card>> LoadCardsAsync(CancellationToken ct)
    {
        var resourceName = $"{typeof(CardSeeder).Namespace}.reflekta_72_cards.json";
        using var stream = typeof(CardSeeder).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        var deck = await JsonSerializer.DeserializeAsync<CardDeckFile>(stream, JsonOptions, ct)
            ?? throw new InvalidOperationException("Card deck file deserialized to null.");

        return deck.Cards.Select(entry => new Card
        {
            Id = Guid.NewGuid(),
            Title = entry.Title,
            Themes = [entry.Theme],
            WisdomText = entry.CoreMessage,
            ReflectionPrompt = entry.ReflectionQuestion,
            BoardPosition = entry.Position - 1,
        }).ToList();
    }

    private sealed record CardDeckFile(List<CardFileEntry> Cards);

    private sealed record CardFileEntry(int Position, string Theme, string Title, string CoreMessage, string ReflectionQuestion);
}
