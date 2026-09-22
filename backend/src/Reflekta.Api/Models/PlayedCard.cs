namespace Reflekta.Api.Models;

public class PlayedCard
{
    public Guid Id { get; set; }
    public Guid JourneyId { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public int SequenceNumber { get; set; }
    public int DiceResult { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}