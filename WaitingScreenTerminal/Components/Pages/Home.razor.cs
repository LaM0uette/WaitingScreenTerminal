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
    
    protected List<string> rendered = new();
    private List<SequenceItem>? items;
    private StreamConfig cfg = new();
    private bool started;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !started)
        {
            started = true;
            cfg = await _streamConfigService.LoadAsync();
            items = await _sequenceService.LoadAsync();
            _ = Run();
        }
    }

    string NowTs() => DateTime.Now.ToString("HH:mm:ss", new System.Globalization.CultureInfo("fr-FR"));

    string ReplaceDyn(string text)
    {
        DateTime start = DateTime.Today.Add(TimeSpan.Parse(cfg.StartTime));
        if (DateTime.Now > start) start = start.AddDays(1);
        TimeSpan remaining = start - DateTime.Now;
        string h = ((int)remaining.TotalHours).ToString("00");
        string m = remaining.Minutes.ToString("00");
        string s = remaining.Seconds.ToString("00");
        text = text.Replace("{remaining}", $"{h}:{m}:{s}");
        text = text.Replace("{startTime}", cfg.StartTime);
        text = cfg.StreamTitle is null ? text.Replace("{title}", "") : text.Replace("{title}", cfg.StreamTitle);
        return text;
    }

    string RenderHtml(SequenceItem it)
    {
        string lvl = it.Level.ToLowerInvariant() switch { "warn" => "warn", "error" => "err", _ => "info" };
        string tag = lvl switch { "warn" => "WARN", "err" => "ERREUR", _ => "INFO" };
        string msg = ReplaceDyn(it.Text);
        return $"<span class='ts'>[{NowTs()}]</span><span class='lvl {lvl}'>[{tag}]</span>{System.Net.WebUtility.HtmlEncode(msg)}";
    }

    async Task Run()
    {
        if (items == null || items.Count == 0) return;
        while (true)
        {
            foreach (SequenceItem it in items)
            {
                if (string.Equals(it.Text.Trim(), "[PAUSE]", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(Math.Max(0, it.DelayMs));
                }
                else if (string.Equals(it.Command, "REBOOT", StringComparison.OrdinalIgnoreCase))
                {
                    rendered.Add(RenderHtml(it));
                    StateHasChanged();
                    await _jsRuntime.InvokeVoidAsync("consoleScroller.toBottom", "console");
                    await Task.Delay(Math.Max(0, it.DelayMs));
                    rendered.Clear();
                    StateHasChanged();
                    break;
                }
                else
                {
                    rendered.Add(RenderHtml(it));
                    StateHasChanged();
                    await _jsRuntime.InvokeVoidAsync("consoleScroller.toBottom", "console");
                    await Task.Delay(Math.Max(0, it.DelayMs));
                }
            }
        }
    }
}