using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reflekta.Api.Controllers;
using Reflekta.Api.Data;
using Reflekta.Api.Services;
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
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("First card."));

        var secondRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);
        var secondResult = await secondRoll.Content.ReadFromJsonAsync<RollResultDto>();
        Assert.Equal(2, secondResult!.SequenceNumber);

        var currentCard = await _client.GetAsync($"/api/journeys/{journey.Id}/current-card");
        var currentResult = await currentCard.Content.ReadFromJsonAsync<RollResultDto>();
        Assert.Equal(secondResult.CardId, currentResult!.CardId);
    }

    [Fact]
    public async Task Roll_AdvancesFromThePreviousCardsBoardPosition()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDiceService>();
                services.AddSingleton<IDiceService>(new FixedDiceService(1));
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "user-1");

        var intentionResponse = await client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("Should I change my career?"));
        var intention = await intentionResponse.Content.ReadFromJsonAsync<IntentionDto>();
        var journeyResponse = await client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intention!.Id));
        var journey = await journeyResponse.Content.ReadFromJsonAsync<JourneyDto>();

        var first = await (await client.PostAsync($"/api/journeys/{journey!.Id}/roll", null)).Content.ReadFromJsonAsync<RollResultDto>();
        await client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("First card."));
        var second = await (await client.PostAsync($"/api/journeys/{journey.Id}/roll", null)).Content.ReadFromJsonAsync<RollResultDto>();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReflektaDbContext>();
        var firstPosition = dbContext.Cards.Single(c => c.Id == first!.CardId).BoardPosition;
        var secondPosition = dbContext.Cards.Single(c => c.Id == second!.CardId).BoardPosition;

        Assert.Equal(1, firstPosition);
        Assert.Equal(2, secondPosition);
    }

    private sealed class FixedDiceService(int value) : IDiceService
    {
        public int Roll() => value;
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


        [Fact]
    public async Task Roll_WithoutReflectingOnThePreviousCard_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        var secondRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);

        Assert.Equal(HttpStatusCode.BadRequest, secondRoll.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_ThenRoll_Succeeds()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        var reflectionResponse = await _client.PostAsJsonAsync(
            $"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("This made me think of my old job."));
        reflectionResponse.EnsureSuccessStatusCode();
        var reflection = await reflectionResponse.Content.ReadFromJsonAsync<ReflectionDto>();
        Assert.Equal("This made me think of my old job.", reflection!.Text);

        var secondRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);
        Assert.True(secondRoll.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SubmitReflection_Twice_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("First."));

        var second = await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Second."));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_WithBlankText_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_BeforeAnyRoll_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journey!.Id}/reflection", new SubmitReflectionRequest("Too early."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        AuthenticateAs("user-2");
        var response = await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Not mine."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

        [Fact]
    public async Task CompleteJourney_AfterReflecting_SetsStatusToCompleted()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Enough for today."));

        var completeResponse = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<JourneyDto>();
        Assert.Equal("Completed", completed!.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task CompleteJourney_WithoutAnyRoll_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        var response = await _client.PostAsync($"/api/journeys/{journey!.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteJourney_WithUnreflectedLatestCard_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        var response = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteJourney_AlreadyCompleted_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Done."));
        await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        var response = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteJourney_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Mine."));

        AuthenticateAs("user-2");
        var response = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


}