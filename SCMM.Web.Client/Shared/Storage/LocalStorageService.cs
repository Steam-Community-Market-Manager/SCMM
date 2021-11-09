using Microsoft.JSInterop;

public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetAsync<T>(string name, T value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", name, value);
    }

    public async Task<T> GetAsync<T>(string name, T defaultValue = default)
    {
        try
        {
            return (await _jsRuntime.InvokeAsync<T>("localStorage.getItem", name)) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task RemoveAsync(string name)
    {
        await _jsRuntime.InvokeAsync<string>("localStorage.removeItem", name);
    }
}
