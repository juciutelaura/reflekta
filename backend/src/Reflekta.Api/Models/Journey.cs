namespace Reflekta.Api.Models;

public enum JourneyStatus
{
    Active,
    Completed
}

public class Journey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid IntentionId { get; set; }
    public Intention? Intention { get; set; }
    public JourneyStatus Status { get; set; } = JourneyStatus.Active;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public List<PlayedCard> PlayedCards { get; set; } = new();
}