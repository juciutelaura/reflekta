using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reflekta.Api.Controllers;
using Reflekta.Api.Data;
using Reflekta.Api.Tests.TestInfrastructure;
using Xunit;

namespace Reflekta.Api.Tests.Controllers;

public class JourneysControllerTests : IAsyncLifetime
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

    private async Task<Guid> CreateIntentionAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("Should I change my career?"));
        var dto = await response.Content.ReadFromJsonAsync<IntentionDto>();
        return dto!.Id;
    }

    [Fact]
    public async Task Roll_PersistsADeterministicCardSelectionAndIncrementsSequence()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        Assert.Equal("Active", journey!.Status);

        var firstRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);
        var firstResult = await firstRoll.Content.ReadFromJsonAsync<RollResultDto>();
        Assert.InRange(firstResult!.DiceResult, 1, 6);
        Assert.Equal(1, firstResult.SequenceNumber);

        var secondRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);
        var secondResult = await secondRoll.Content.ReadFromJsonAsync<RollResultDto>();
        Assert.Equal(2, secondResult!.SequenceNumber);

        var currentCard = await _client.GetAsync($"/api/journeys/{journey.Id}/current-card");
        var currentResult = await currentCard.Content.ReadFromJsonAsync<RollResultDto>();
        Assert.Equal(secondResult.CardId, currentResult!.CardId);
    }

    [Fact]
    public async Task Roll_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        AuthenticateAs("user-2");
        var rollResponse = await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        Assert.Equal(HttpStatusCode.Forbidden, rollResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithAnotherUsersIntention_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();

        AuthenticateAs("user-2");
        var response = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CurrentCard_BeforeAnyRoll_ReturnsNotFound()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        var response = await _client.GetAsync($"/api/journeys/{journey!.Id}/current-card");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}