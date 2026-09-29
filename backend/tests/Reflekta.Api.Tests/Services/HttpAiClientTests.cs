using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Reflekta.Api.Services;

namespace Reflekta.Api.Tests.Services;

public class HttpAiClientTests
{
    private static readonly ReflectionContext Context = new(
        "Should I change my career?",
        new AiCard("Control", "Notice where you hold on tightly.", "What are you trying to control?"),
        "I grip plans tightly.",
        [new AiMessage("assistant", "What stands out?"), new AiMessage("user", "The gripping.")]);

    private static HttpAiClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://ai.test") },
            Options.Create(new AiServiceOptions { BaseUrl = "http://ai.test", InternalKey = "secret" }));

    [Fact]
    public async Task RespondAsync_PostsCamelCaseContextWithInternalKey_AndReturnsReply()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{ "reply": "When did that start?" }""");

        var reply = await CreateClient(handler).RespondAsync(Context, CancellationToken.None);

        Assert.Equal("When did that start?", reply);
        Assert.Equal("http://ai.test/ai/reflection/respond", handler.Request!.RequestUri!.ToString());
        Assert.Equal("secret", handler.Request.Headers.GetValues("X-Internal-Key").Single());
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Should I change my career?", body.RootElement.GetProperty("intention").GetString());
        Assert.Equal("Notice where you hold on tightly.", body.RootElement.GetProperty("card").GetProperty("wisdomText").GetString());
        Assert.Equal("user", body.RootElement.GetProperty("messages")[1].GetProperty("role").GetString());
    }

    [Fact]
    public async Task RespondAsync_OnErrorStatus_ThrowsAiUnavailable()
    {
        var handler = new StubHandler(HttpStatusCode.BadGateway, "{}");

        await Assert.ThrowsAsync<AiUnavailableException>(() => CreateClient(handler).RespondAsync(Context, CancellationToken.None));
    }

    [Fact]
    public async Task RespondAsync_OnBlankReply_ThrowsAiUnavailable()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{ "reply": "   " }""");

        await Assert.ThrowsAsync<AiUnavailableException>(() => CreateClient(handler).RespondAsync(Context, CancellationToken.None));
    }

    [Fact]
    public async Task RespondAsync_WhenServiceIsUnreachable_ThrowsAiUnavailable()
    {
        var handler = new StubHandler(new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<AiUnavailableException>(() => CreateClient(handler).RespondAsync(Context, CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody = "";
        private readonly Exception? _exception;

        public StubHandler(HttpStatusCode status, string responseBody) => (_status, _responseBody) = (status, responseBody);
        public StubHandler(Exception exception) => _exception = exception;

        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (_exception is not null)
                throw _exception;

            Request = request;
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(_status) { Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json") };
        }
    }
}
