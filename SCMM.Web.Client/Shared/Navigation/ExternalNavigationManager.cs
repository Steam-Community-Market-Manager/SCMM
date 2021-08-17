using Microsoft.JSInterop;

public class ExternalNavigationManager
{
    private readonly IJSRuntime _jsRuntime;

    public ExternalNavigationManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public void NavigateTo(string uri)
    {
        _jsRuntime.InvokeVoidAsync("WindowInterop.open", uri);
    }

    public void NavigateToNewTab(string uri)
    {
        _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", uri);
    }
}
