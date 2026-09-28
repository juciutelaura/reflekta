namespace Reflekta.Api.Models;

public static class ConversationRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid PlayedCardId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
