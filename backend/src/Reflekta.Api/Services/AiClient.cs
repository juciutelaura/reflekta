using System.Net.Http.Json;
using Microsoft.Extensions.Options;


namespace Reflekta.Api.Services;
public record SummaryCard(string Title, string WisdomText);
public record SummaryPlayedCard(SummaryCard Card, string ReflectionText, List<AiMessage> Messages);
public record SummaryContext(string Intention, List<SummaryPlayedCard> PlayedCards);
public record SessionSummaryResult(string SummaryText, List<string> Themes);
public record AiCard(string Title, string WisdomText, string ReflectionPrompt);
public record AiMessage(string Role, string Content);
public record ReflectionContext(string Intention, AiCard Card, string Reflection, List<AiMessage> Messages);


public interface IAiClient
{
    Task<string> RespondAsync(ReflectionContext context, CancellationToken ct);
    Task<SessionSummaryResult> SummarizeAsync(SummaryContext context, CancellationToken ct);
}


/// <summary>The AI service could not produce a reply (unreachable, timeout, error status, or empty reply).</summary>
public class AiUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public class AiServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string InternalKey { get; set; } = string.Empty;
}

/// <summary>Calls the internal Python AI service. Never logs or exposes user text.</summary>
public class HttpAiClient(HttpClient httpClient, IOptions<AiServiceOptions> options) : IAiClient
{
    private record ReplyResponse(string? Reply);

    public async Task<string> RespondAsync(ReflectionContext context, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/reflection/respond")
        {
            Content = JsonContent.Create(context)
        };
        request.Headers.Add("X-Internal-Key", options.Value.InternalKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new AiUnavailableException($"AI service returned status {(int)response.StatusCode}.");

            var body = await response.Content.ReadFromJsonAsync<ReplyResponse>(ct);
            if (string.IsNullOrWhiteSpace(body?.Reply))
                throw new AiUnavailableException("AI service returned an empty reply.");

            return body.Reply.Trim();
        }
        catch (HttpRequestException ex)
        {
            throw new AiUnavailableException("AI service is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AiUnavailableException("AI service timed out.", ex);
        }
    }


        public async Task<SessionSummaryResult> SummarizeAsync(SummaryContext context, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/ai/session/summarize")
        {
            Content = JsonContent.Create(context)
        };
        request.Headers.Add("X-Internal-Key", options.Value.InternalKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new AiUnavailableException($"AI service returned status {(int)response.StatusCode}.");

            var body = await response.Content.ReadFromJsonAsync<SummaryReplyBody>(ct);
            if (string.IsNullOrWhiteSpace(body?.SummaryText))
                throw new AiUnavailableException("AI service returned an empty summary.");

            return new SessionSummaryResult(body.SummaryText.Trim(), body.Themes ?? new List<string>());
        }
        catch (HttpRequestException ex)
        {
            throw new AiUnavailableException("AI service is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AiUnavailableException("AI service timed out.", ex);
        }
    }

    private record SummaryReplyBody(string? SummaryText, List<string>? Themes);




}
