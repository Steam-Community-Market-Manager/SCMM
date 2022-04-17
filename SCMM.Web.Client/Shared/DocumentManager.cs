using Microsoft.JSInterop;

namespace SCMM.Web.Client.Shared;

public class DocumentManager
{
    private readonly IJSRuntime _jsRuntime;

    public DocumentManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public void ScrollElementIntoView(string selector, TimeSpan? delay = null, int maxRetries = 10)
    {
        if (delay != null)
        {
            Timer timer = null;
            timer = new Timer(async x =>
            {
                try
                {
                    if (!(await _jsRuntime.InvokeAsync<bool>("WindowInterop.scrollElementIntoView", selector)))
                    {
                        // Element might not be visible yet, retry later...
                        if (delay != null && maxRetries > 0)
                        {
                            ScrollElementIntoView(selector, delay, maxRetries - 1);
                        }
                    }
                }
                finally
                {
                    timer?.Dispose();
                }
            });

            timer?.Change(((int?)delay?.TotalMilliseconds) ?? 0, Timeout.Infinite);
        }
        else
        {
            _jsRuntime.InvokeVoidAsync("WindowInterop.scrollElementIntoView", selector);
        }
    }
}