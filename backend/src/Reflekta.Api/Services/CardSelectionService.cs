using Reflekta.Api.Models;

namespace Reflekta.Api.Services;

public record CardSelectionResult(int Position, Card Card);

public interface ICardSelectionService
{
    CardSelectionResult SelectNext(int currentPosition, int diceResult, IReadOnlyList<Card> availableCards);
}

public class CardSelectionService : ICardSelectionService
{
    public CardSelectionResult SelectNext(int currentPosition, int diceResult, IReadOnlyList<Card> availableCards)
    {
        if (availableCards.Count == 0)
            throw new InvalidOperationException("No cards configured for the board.");

        var boardSize = availableCards.Count;
        var newPosition = (currentPosition + diceResult) % boardSize;

        var card = availableCards.SingleOrDefault(c => c.BoardPosition == newPosition)
            ?? throw new InvalidOperationException($"No card configured for board position {newPosition}.");

        return new CardSelectionResult(newPosition, card);
    }
}