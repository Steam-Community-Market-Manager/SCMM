using Microsoft.JSInterop;

public class DocumentManager
{
    private readonly IJSRuntime _jsRuntime;

    public DocumentManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public void ScrollElementIntoView(string selector, TimeSpan? delay = null)
    {
        if (delay != null)
        {
            _ = new Timer(async x =>
            {
                await _jsRuntime.InvokeVoidAsync("WindowInterop.scrollElementIntoView", selector);
            },
                null,
                ((int?)delay?.TotalMilliseconds) ?? 0,
                Timeout.Infinite
            );
        }
        else
        {
            _jsRuntime.InvokeVoidAsync("WindowInterop.scrollElementIntoView", selector);
        }
    }
}