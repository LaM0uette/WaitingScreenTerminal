using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using Services;

namespace Pages;

public class HomeBase : ComponentBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IStreamConfigService StreamConfigService { get; set; } = null!;
    [Inject] public ISequenceService SequenceService { get; set; } = null!;

    private readonly System.Globalization.CultureInfo _fr = new("fr-FR");
    protected readonly List<LogLine> _lines = new();
    private List<SequenceItem> _sequence = new();
    private StreamConfig _config = new();
    private bool _started;
    private CancellationTokenSource? _cts;
    private Task? _runner;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started) return;

        _config = await StreamConfigService.LoadAsync();
        _sequence = await SequenceService.LoadAsync();

        _cts = new CancellationTokenSource();
        _runner = RunLoopAsync(_cts.Token);
        _started = true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts?.Cancel();
            if (_runner is not null) await _runner;
        }
        catch { }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _runner = null;
            _started = false;
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        if (_sequence.Count == 0) return;

        while (!ct.IsCancellationRequested)
        {
            foreach (SequenceItem item in _sequence)
            {
                if (ct.IsCancellationRequested) break;

                if (IsPause(item))
                {
                    await SafeDelay(item.DelayMs, ct);
                }
                else if (IsReboot(item))
                {
                    AppendLine(item);
                    await SafeDelay(item.DelayMs, ct);
                    _lines.Clear();
                    await InvokeAsync(StateHasChanged);
                    break;
                }
                else
                {
                    AppendLine(item);
                    await SafeDelay(item.DelayMs, ct);
                }

                await SafeScrollAsync(ct);
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
        _ = InvokeAsync(StateHasChanged);
    }

    private string BuildMessage(string template)
    {
        DateTime start = DateTime.Today.Add(TimeSpan.Parse(_config.StartTime));
        if (DateTime.Now > start) start = start.AddDays(1);
        TimeSpan remaining = start - DateTime.Now;
        string remainingText = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";

        return template
            .Replace("{startTime}", _config.StartTime)
            .Replace("{remaining}", remainingText)
            .Replace("{title}", _config.StreamTitle ?? string.Empty);
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

    private static async Task SafeDelay(int ms, CancellationToken ct)
    {
        try { await Task.Delay(Math.Max(0, ms), ct); } catch (OperationCanceledException) { }
    }

    private async Task SafeScrollAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        try { await JsRuntime.InvokeVoidAsync("consoleScroller.toBottom", "console"); }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }
}
