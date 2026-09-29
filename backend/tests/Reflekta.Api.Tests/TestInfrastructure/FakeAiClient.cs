using Reflekta.Api.Services;

namespace Reflekta.Api.Tests.TestInfrastructure;

public class FakeAiClient : IAiClient
{
    public string Reply { get; set; } = "What stands out to you?";
    public bool Fail { get; set; }
    public List<ReflectionContext> Requests { get; } = new();

    public Task<string> RespondAsync(ReflectionContext context, CancellationToken ct)
    {
        Requests.Add(context);
        if (Fail)
            throw new AiUnavailableException("Fake AI failure.");
        return Task.FromResult(Reply);
    }
}
