namespace Models;

public record SequenceItem(string Text, int DelayMs, string Level = "info", string? Command = null);