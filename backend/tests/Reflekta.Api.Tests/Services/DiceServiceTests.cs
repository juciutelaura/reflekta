using Reflekta.Api.Services;
using Xunit;

namespace Reflekta.Api.Tests.Services;

public class DiceServiceTests
{
    [Fact]
    public void Roll_AlwaysReturnsValueBetweenOneAndSix()
    {
        var service = new DiceService();

        for (var i = 0; i < 1000; i++)
        {
            var result = service.Roll();
            Assert.InRange(result, 1, 6);
        }
    }

    [Fact]
    public void Roll_UsesInjectedRandomSourceWhenProvided()
    {
        var fixedRandom = new Random(42);
        var expected = fixedRandom.Next(1, 7);

        var service = new DiceService(new Random(42));
        var actual = service.Roll();

        Assert.Equal(expected, actual);
    }
}
