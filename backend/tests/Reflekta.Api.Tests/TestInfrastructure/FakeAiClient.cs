using Reflekta.Api.Services;

namespace Reflekta.Api.Tests.TestInfrastructure;

public class FakeAiClient : IAiClient
{
    public string Reply { get; set; } = "What stands out to you?";
    public bool Fail { get; set; }
    public List<ReflectionContext> Requests { get; } = new();

    public string SummaryText { get; set; } = "The user explored what it means to hold on tightly to plans.";
    public List<string> SummaryThemes { get; set; } = new() { "control" };
    public bool SummaryFail { get; set; }
    public List<SummaryContext> SummaryRequests { get; } = new();

    public Task<string> RespondAsync(ReflectionContext context, CancellationToken ct)
    {
        Requests.Add(context);
        if (Fail)
            throw new AiUnavailableException("Fake AI failure.");
        return Task.FromResult(Reply);
    }

    public Task<SessionSummaryResult> SummarizeAsync(SummaryContext context, CancellationToken ct)
    {
        SummaryRequests.Add(context);
        if (SummaryFail)
            throw new AiUnavailableException("Fake AI failure.");
        return Task.FromResult(new SessionSummaryResult(SummaryText, SummaryThemes));
    }
}
