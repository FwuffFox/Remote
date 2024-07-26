namespace Remote.Models;

public class Message
{
    public int Id { get; set; }
    public required string Text { get; set; } = string.Empty;
    public required User User { get; set; }
}