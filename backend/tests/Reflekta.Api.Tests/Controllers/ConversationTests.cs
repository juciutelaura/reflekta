using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

        [Fact]
    public async Task SendMessage_SavesBothMessagesAndSendsTheConversationToTheAi()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);
        _factory.AiClient.Reply = "When did that start?";

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest("Since school."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SendMessageResponse>();
        Assert.Equal("user", result!.UserMessage.Role);
        Assert.Equal("Since school.", result.UserMessage.Content);
        Assert.Equal("When did that start?", result.AssistantMessage.Content);

        var lastRequest = _factory.AiClient.Requests.Last();
        Assert.Equal(["assistant", "user"], lastRequest.Messages.Select(m => m.Role));
        Assert.Equal("Since school.", lastRequest.Messages.Last().Content);

        var current = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Equal(["assistant", "user", "assistant"], current!.Messages.Select(m => m.Role));
    }

    [Fact]
    public async Task SendMessage_WhenAiFails_Returns503AndKeepsTheUserMessage()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);
        _factory.AiClient.Fail = true;

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest("Since school."));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var current = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Equal("user", current!.Messages.Last().Role);
        Assert.Equal("Since school.", current.Messages.Last().Content);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SendMessage_WithBlankContent_ReturnsBadRequest(string? content)
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest(content!));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_LongerThan2000Characters_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest(new string('a', 2001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_BeforeReflecting_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest("Hello"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_OnCompletedJourney_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);
        await _client.PostAsync($"/api/journeys/{journeyId}/complete", null);

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest("Hello"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);

        AuthenticateAs("user-2");
        var response = await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest("Not mine."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_OnUnknownJourney_ReturnsNotFound()
    {
        AuthenticateAs("user-1");

        var response = await _client.PostAsJsonAsync($"/api/journeys/{Guid.NewGuid()}/messages", new SendMessageRequest("Hello"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestReply_AfterAFailedFirstTurn_AnswersTheReflection()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        _factory.AiClient.Fail = true;
        await ReflectAsync(journeyId);
        _factory.AiClient.Fail = false;
        _factory.AiClient.Reply = "What stands out to you?";

        var response = await _client.PostAsync($"/api/journeys/{journeyId}/messages/reply", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        Assert.Equal("What stands out to you?", result!.AssistantMessage.Content);
        var current = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Single(current!.Messages);
    }

    [Fact]
    public async Task RequestReply_AfterAFailedMessage_AnswersTheSavedUserMessageWithoutDuplicatingIt()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);
        _factory.AiClient.Fail = true;
        await _client.PostAsJsonAsync($"/api/journeys/{journeyId}/messages", new SendMessageRequest("Since school."));
        _factory.AiClient.Fail = false;

        var response = await _client.PostAsync($"/api/journeys/{journeyId}/messages/reply", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var current = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Equal(["assistant", "user", "assistant"], current!.Messages.Select(m => m.Role));
    }

    [Fact]
    public async Task RequestReply_WhenTheLastMessageIsFromTheAssistant_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        await ReflectAsync(journeyId);

        var response = await _client.PostAsync($"/api/journeys/{journeyId}/messages/reply", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var current = await _client.GetFromJsonAsync<CurrentCardDto>($"/api/journeys/{journeyId}/current-card");
        Assert.Single(current!.Messages);
    }

    [Fact]
    public async Task RequestReply_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var journeyId = await StartJourneyAndRollAsync();
        _factory.AiClient.Fail = true;
        await ReflectAsync(journeyId);

        AuthenticateAs("user-2");
        var response = await _client.PostAsync($"/api/journeys/{journeyId}/messages/reply", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}
