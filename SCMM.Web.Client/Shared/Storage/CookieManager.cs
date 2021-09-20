using Microsoft.JSInterop;

public class CookieManager : ICookieManager
{
    private readonly IJSRuntime _jsRuntime;

    public CookieManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public virtual async Task SetAsync<T>(string name, T value, int days = 365)
    {
        await _jsRuntime.InvokeVoidAsync("CookieInterop.setCookie", name, value, days);
    }

    public virtual async Task<T> GetAsync<T>(string name, T defaultValue = default(T))
    {
        try
        {
            return (await _jsRuntime.InvokeAsync<T>("CookieInterop.getCookie", name)) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public virtual async Task RemoveAsync(string name)
    {
        await _jsRuntime.InvokeAsync<string>("CookieInterop.removeCookie", name);
    }
}