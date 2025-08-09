namespace Models;

public enum LogLevel { Info, Warn, Error }

public class LogLine
{
    public string Timestamp { get; set; } = "";
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Message { get; set; } = "";
}