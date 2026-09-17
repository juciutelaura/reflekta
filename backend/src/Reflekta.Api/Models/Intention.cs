namespace Reflekta.Api.Models;

public class Intention
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string? ClarifiedText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}