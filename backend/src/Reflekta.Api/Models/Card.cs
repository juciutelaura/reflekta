namespace Reflekta.Api.Models;

public class Card
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WisdomText { get; set; } = string.Empty;
    public string ReflectionPrompt { get; set; } = string.Empty;
    public List<string> Themes { get; set; } = new();
    public int BoardPosition { get; set; }
}