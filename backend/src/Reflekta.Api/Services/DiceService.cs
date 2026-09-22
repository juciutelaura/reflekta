namespace Reflekta.Api.Services;

public interface IDiceService
{
    int Roll();
}

public class DiceService : IDiceService
{
    private readonly Random _random;

    public DiceService() : this(Random.Shared) { }

    public DiceService(Random random)
    {
        _random = random;
    }

    public int Roll() => _random.Next(1, 7);
}
