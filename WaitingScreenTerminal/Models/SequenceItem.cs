namespace Models;

public class SequenceItem
{
    public string Text { get; set; } = "";
    public int DelayMs { get; set; }
    public string Level { get; set; } = "info";
    public string? Command { get; set; }
}