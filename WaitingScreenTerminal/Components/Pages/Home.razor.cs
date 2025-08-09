using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using Services;

namespace Pages;

public class HomeBase : ComponentBase
{
    #region Statements

    protected readonly List<string> Lines = [];
    
    [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;
    [Inject] private ISequenceService _sequenceService { get; set; } = null!;
    
    private List<SequenceItem>? _items;
    private bool _started;
    private bool _loop = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_started)
        {
            _started = true;
            _items = await _sequenceService.LoadAsync();
            _ = Run();
        }
    }
    
    #endregion

    #region Methods

    private async Task Run()
    {
        if (_items == null || _items.Count == 0) 
            return;
        
        while (true)
        {
            foreach (SequenceItem it in _items)
            {
                if (it.Text.Trim().Equals("[PAUSE]", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(Math.Max(0, it.DelayMs));
                }
                else
                {
                    Lines.Add(it.Text);
                    StateHasChanged();
                    await _jsRuntime.InvokeVoidAsync("consoleScroller.toBottom", "console");
                    await Task.Delay(Math.Max(0, it.DelayMs));
                }
            }
            if (!_loop) break;
        }
    }

    #endregion
}