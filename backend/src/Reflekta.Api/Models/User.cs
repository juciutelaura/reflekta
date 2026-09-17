namespace Reflekta.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string ExternalAuthId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}