using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;

namespace Reflekta.Api.Tests.Data;

public class SessionSummaryPersistenceTests
{
    [Fact]
    public async Task SavesAndLoadsASessionSummaryForAJourney()
    {
        var options = new DbContextOptionsBuilder<ReflektaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var journeyId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();

        using (var writeContext = new ReflektaDbContext(options))
        {
            writeContext.SessionSummaries.Add(new SessionSummary
            {
                Id = summaryId,
                JourneyId = journeyId,
                SummaryText = "The user explored what it means to hold on tightly to plans.",
                Themes = new List<string> { "control", "career" },
                CreatedAt = DateTimeOffset.UtcNow
            });
            await writeContext.SaveChangesAsync();
        }

        using var readContext = new ReflektaDbContext(options);
        var loaded = await readContext.SessionSummaries.SingleAsync(s => s.Id == summaryId);

        Assert.Equal(journeyId, loaded.JourneyId);
        Assert.Equal("The user explored what it means to hold on tightly to plans.", loaded.SummaryText);
        Assert.Equal(new List<string> { "control", "career" }, loaded.Themes);
    }
}
