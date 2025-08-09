namespace Models;

public class LogLine
{
    public string Timestamp { get; init; } = string.Empty;
    public LogLevel Level { get; init; } = LogLevel.Info;
    public string Message { get; init; } = string.Empty;
}