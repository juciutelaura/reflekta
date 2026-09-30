namespace Reflekta.Api.Models;

public class SessionSummary
{
    public Guid Id { get; set; }
    public Guid JourneyId { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public List<string> Themes { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
