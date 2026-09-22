using Reflekta.Api.Models;
using Reflekta.Api.Services;
using Xunit;

namespace Reflekta.Api.Tests.Services;

public class CardSelectionServiceTests
{
    private static List<Card> SixPositionBoard() => new()
    {
        new Card { Id = Guid.NewGuid(), Title = "A", WisdomText = "a", BoardPosition = 0 },
        new Card { Id = Guid.NewGuid(), Title = "B", WisdomText = "b", BoardPosition = 1 },
        new Card { Id = Guid.NewGuid(), Title = "C", WisdomText = "c", BoardPosition = 2 },
        new Card { Id = Guid.NewGuid(), Title = "D", WisdomText = "d", BoardPosition = 3 },
        new Card { Id = Guid.NewGuid(), Title = "E", WisdomText = "e", BoardPosition = 4 },
        new Card { Id = Guid.NewGuid(), Title = "F", WisdomText = "f", BoardPosition = 5 },
    };

    [Fact]
    public void SelectNext_IsDeterministic_SameInputsAlwaysProduceSameResult()
    {
        var service = new CardSelectionService();
        var board = SixPositionBoard();

        var first = service.SelectNext(currentPosition: 0, diceResult: 4, board);
        var second = service.SelectNext(currentPosition: 0, diceResult: 4, board);

        Assert.Equal(first.Position, second.Position);
        Assert.Equal(first.Card.Id, second.Card.Id);
    }

    [Fact]
    public void SelectNext_WrapsAroundTheBoard()
    {
        var service = new CardSelectionService();
        var board = SixPositionBoard();

        var result = service.SelectNext(currentPosition: 4, diceResult: 3, board);

        Assert.Equal(1, result.Position); // (4 + 3) % 6 == 1
        Assert.Equal("B", result.Card.Title);
    }

    [Fact]
    public void SelectNext_ThrowsWhenNoCardsConfigured()
    {
        var service = new CardSelectionService();

        Assert.Throws<InvalidOperationException>(() =>
            service.SelectNext(0, 3, new List<Card>()));
    }
}
