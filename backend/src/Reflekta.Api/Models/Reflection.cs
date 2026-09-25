namespace Reflekta.Api.Models;

public class Reflection
{
    public Guid Id { get; set; }
    public Guid PlayedCardId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
