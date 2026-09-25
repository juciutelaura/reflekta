using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reflekta.Api.Controllers;
using Reflekta.Api.Data;
using Reflekta.Api.Tests.TestInfrastructure;
using Xunit;
using static Reflekta.Api.Controllers.JourneysController;

namespace Reflekta.Api.Tests.Controllers;

public class JourneyHistoryTests : IAsyncLifetime
{
    private readonly ReflektaWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReflektaDbContext>();
        await CardSeeder.SeedAsync(dbContext);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private void AuthenticateAs(string userId)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", userId);
    }

    private async Task<Guid> CreateIntentionAsync(string text)
    {
        var response = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest(text));
        var dto = await response.Content.ReadFromJsonAsync<IntentionDto>();
        return dto!.Id;
    }

    private async Task<Guid> CreateReflectedJourneyAsync(string intentionText)
    {
        var intentionId = await CreateIntentionAsync(intentionText);
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("A reflection."));
        return journey.Id;
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCurrentUsersJourneys_NewestFirst()
    {
        AuthenticateAs("user-1");
        var firstId = await CreateReflectedJourneyAsync("First intention");
        var secondId = await CreateReflectedJourneyAsync("Second intention");

        AuthenticateAs("user-2");
        await CreateReflectedJourneyAsync("Someone else's intention");

        AuthenticateAs("user-1");
        var response = await _client.GetAsync("/api/journeys");

        response.EnsureSuccessStatusCode();
        var journeys = await response.Content.ReadFromJsonAsync<List<JourneySummaryDto>>();

        Assert.Equal(2, journeys!.Count);
        Assert.Equal(secondId, journeys[0].Id);
        Assert.Equal(firstId, journeys[1].Id);
        Assert.Equal("Second intention", journeys[0].IntentionText);
        Assert.Equal(1, journeys[0].CardCount);
    }

    [Fact]
    public async Task GetById_ReturnsPlayedCardsWithTheirReflections()
    {
        AuthenticateAs("user-1");
        var journeyId = await CreateReflectedJourneyAsync("My intention");

        var response = await _client.GetAsync($"/api/journeys/{journeyId}");

        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<JourneyDetailDto>();

        Assert.Equal("My intention", detail!.IntentionText);
        Assert.Single(detail.PlayedCards);
        Assert.Equal("A reflection.", detail.PlayedCards[0].ReflectionText);
        Assert.False(string.IsNullOrEmpty(detail.PlayedCards[0].CardTitle));
    }

    [Fact]
    public async Task GetById_ForAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var journeyId = await CreateReflectedJourneyAsync("Mine");

        AuthenticateAs("user-2");
        var response = await _client.GetAsync($"/api/journeys/{journeyId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownJourney_ReturnsNotFound()
    {
        AuthenticateAs("user-1");

        var response = await _client.GetAsync($"/api/journeys/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
