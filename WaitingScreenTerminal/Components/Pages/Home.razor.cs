using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using Services;

namespace Pages;

public class HomeBase : ComponentBase, IAsyncDisposable
{
    #region Statements
    
    protected readonly List<LogLine> LogLines = [];

    [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;
    [Inject] private IStreamConfigService _streamConfigService { get; set; } = null!;
    [Inject] private ISequenceService _sequenceService { get; set; } = null!;
    
    private StreamConfig _streamConfig = new();
    private List<SequenceItem> _sequenceItems = [];
    private bool _started;
    private CancellationTokenSource? _cancellationTokenSource;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started) 
            return;

        _streamConfig = await _streamConfigService.LoadAsync();
        _sequenceItems = await _sequenceService.LoadAsync();

        _cancellationTokenSource = new CancellationTokenSource();
        _started = true;
        
        _ = PlaySequenceAsync(_cancellationTokenSource.Token);
    }

    #endregion

    #region Methods
    
    protected string GetLevelClass(LogLevel level)
    {
        return level switch
        {
            LogLevel.Warn => "warn",
            LogLevel.Error => "error",
            _ => "info"
        };
    }

    protected string GetLevelTag(LogLevel level)
    {
        return level switch
        {
            LogLevel.Warn => "WARN",
            LogLevel.Error => "ERR",
            _ => "INFO"
        };
    }


    private async Task PlaySequenceAsync(CancellationToken cancellationToken)
    {
        if (_sequenceItems.Count == 0) 
            return;

        foreach (SequenceItem item in _sequenceItems)
        {
            if (cancellationToken.IsCancellationRequested) 
                return;

            if (IsPause(item))
            {
                await SafeDelay(item.DelayMs, cancellationToken);
            }
            else if (IsReboot(item))
            {
                await AppendLine(item, cancellationToken);
                LogLines.Clear();
                
                await InvokeAsync(StateHasChanged);
                
                // reboot
                if (!cancellationToken.IsCancellationRequested)
                {
                    _ = PlaySequenceAsync(cancellationToken);
                }
                
                return;
            }
            else
            {
                await AppendLine(item, cancellationToken);
            }

            await SafeScrollAsync(cancellationToken);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            _ = PlaySequenceAsync(cancellationToken);
        }
    }
    
    private bool IsPause(SequenceItem item) => string.Equals(item.Text.Trim(), "[PAUSE]", StringComparison.OrdinalIgnoreCase);
    private bool IsReboot(SequenceItem item) => string.Equals(item.Command, "REBOOT", StringComparison.OrdinalIgnoreCase);

    private async Task AppendLine(SequenceItem item, CancellationToken cancellationToken)
    {
        LogLine line = new()
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss", new CultureInfo("fr-FR")),
            Level = ParseLevel(item.Level),
            Message = BuildMessage(item.Text)
        };

        LogLines.Add(line);
        _ = InvokeAsync(StateHasChanged);
        
        await SafeDelay(item.DelayMs, cancellationToken);
    }
    
    private LogLevel ParseLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "warn" => LogLevel.Warn,
            "error" => LogLevel.Error,
            _ => LogLevel.Info
        };
    }

    private string BuildMessage(string template)
    {
        DateTime start = DateTime.Today.Add(TimeSpan.Parse(_streamConfig.StartTime));
        
        if (DateTime.Now > start)
        {
            start = start.AddDays(1);
        }
        
        TimeSpan remaining = start - DateTime.Now;
        string remainingText = $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";

        return template
            .Replace("{startTime}", _streamConfig.StartTime)
            .Replace("{remaining}", remainingText)
            .Replace("{title}", _streamConfig.StreamTitle ?? string.Empty);
    }
    
    private async Task SafeDelay(int delayMs, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Math.Max(0, delayMs), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private async Task SafeScrollAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        try { await _jsRuntime.InvokeVoidAsync("consoleScroller.toBottom", "console"); }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    #endregion

    #region IAsyncDisposable

    public ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _started = false;
        
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    #endregion
}
