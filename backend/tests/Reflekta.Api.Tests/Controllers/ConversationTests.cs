using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reflekta.Api.Controllers;
using Reflekta.Api.Data;
using Reflekta.Api.Tests.TestInfrastructure;

namespace Reflekta.Api.Tests.Controllers;

public class ConversationTests : IAsyncLifetime
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

    private async Task<Guid> StartJourneyAndRollAsync()
    {
        var intentionResponse = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("Should I change my career?"));
        var intention = await intentionResponse.Content.ReadFromJsonAsync<IntentionDto>();
        var journeyResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intention!.Id));
        var journey = await journeyResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        return journey.Id;
    }

    private async Task<ReflectionWithConversationDto> ReflectAsync(Guid journeyId, string text = "I grip plans tightly.")
    {
        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/reflection", new SubmitReflectionRequest(text));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReflectionWithConversationDto>())!;
    }

    [Fact]
    public async Task SubmitReflection_SavesAndReturnsTheFirstAssistantMessage()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        _factory.AiClient.Reply = "What stands out to you?";

        var result = await ReflectAsync(journeyId);

        Assert.False(result.AiUnavailable);
        var message = Assert.Single(result.Messages);
        Assert.Equal("assistant", message.Role);
        Assert.Equal("What stands out to you?", message.Content);

        var request = Assert.Single(_factory.AiClient.Requests);
        Assert.Equal("Should I change my career?", request.Intention);
        Assert.Equal("I grip plans tightly.", request.Reflection);
        Assert.False(string.IsNullOrWhiteSpace(request.Card.WisdomText));
        Assert.Empty(request.Messages);
    }

    [Fact]
    public async Task SubmitReflection_WhenAiFails_StillSavesTheReflection()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        _factory.AiClient.Fail = true;

        var result = await ReflectAsync(journeyId);

        Assert.True(result.AiUnavailable);
        Assert.Empty(result.Messages);
        Assert.Equal("I grip plans tightly.", result.Text);

        var current = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Equal("I grip plans tightly.", current!.ReflectionText);
    }

    [Fact]
    public async Task GetCurrentCard_ReturnsReflectionAndConversation()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();

        var before = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Null(before!.ReflectionText);
        Assert.Empty(before.Messages);

        await ReflectAsync(journeyId);

        var after = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Equal("I grip plans tightly.", after!.ReflectionText);
        var message = Assert.Single(after.Messages);
        Assert.Equal("assistant", message.Role);
    }
}
