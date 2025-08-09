using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using Services;

namespace Pages;

public class HomeBase : ComponentBase
{
    [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;
    [Inject] private IStreamConfigService _streamConfigService { get; set; } = null!;
    [Inject] private ISequenceService _sequenceService { get; set; } = null!;
    
    private readonly System.Globalization.CultureInfo _fr = new("fr-FR");
    protected List<LogLine> _lines = new();
    private List<SequenceItem> _sequence = new();
    private StreamConfig _config = new();
    private bool _started;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_started)
        {
            _started = true;
            _config = await _streamConfigService.LoadAsync();
            _sequence = await _sequenceService.LoadAsync();
            _ = RunLoop();
        }
    }

    private async Task RunLoop()
    {
        if (_sequence.Count == 0) return;

        while (true)
        {
            foreach (var item in _sequence)
            {
                if (IsPause(item))
                {
                    await Task.Delay(Math.Max(0, item.DelayMs));
                }
                else if (IsReboot(item))
                {
                    AppendLine(item);
                    await Task.Delay(Math.Max(0, item.DelayMs));
                    _lines.Clear();
                    StateHasChanged();
                    break;
                }
                else
                {
                    AppendLine(item);
                    await Task.Delay(Math.Max(0, item.DelayMs));
                }

                await _jsRuntime.InvokeVoidAsync("consoleScroller.toBottom", "console");
            }
        }
    }

    private void AppendLine(SequenceItem item)
    {
        var line = new LogLine
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss", _fr),
            Level = ParseLevel(item.Level),
            Message = BuildMessage(item.Text)
        };

        _lines.Add(line);
        StateHasChanged();
    }

    private string BuildMessage(string template)
    {
        var startToday = DateTime.Today.Add(TimeSpan.Parse(_config.StartTime));
        if (DateTime.Now > startToday) startToday = startToday.AddDays(1);
        var remaining = startToday - DateTime.Now;

        var remainingText = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";

        var message = template
            .Replace("{startTime}", _config.StartTime)
            .Replace("{remaining}", remainingText)
            .Replace("{title}", _config.StreamTitle ?? string.Empty);

        return message;
    }

    private static bool IsPause(SequenceItem item) =>
        string.Equals(item.Text.Trim(), "[PAUSE]", StringComparison.OrdinalIgnoreCase);

    private static bool IsReboot(SequenceItem item) =>
        string.Equals(item.Command, "REBOOT", StringComparison.OrdinalIgnoreCase);

    private static LogLevel ParseLevel(string level) =>
        level?.ToLowerInvariant() switch
        {
            "warn" => LogLevel.Warn,
            "error" => LogLevel.Error,
            _ => LogLevel.Info
        };

    protected static string GetLevelClass(LogLevel level) =>
        level switch
        {
            LogLevel.Warn => "warn",
            LogLevel.Error => "err",
            _ => "info"
        };

    protected static string GetLevelTag(LogLevel level) =>
        level switch
        {
            LogLevel.Warn => "WARN",
            LogLevel.Error => "ERREUR",
            _ => "INFO"
        };
}