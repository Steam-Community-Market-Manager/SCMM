﻿using Microsoft.JSInterop;
using SCMM.Web.Data.Models.Services;

namespace SCMM.Web.Client.Services;

public class JavascriptCookieManager : ICookieManager
{
    private readonly IJSRuntime _jsRuntime;

    public JavascriptCookieManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public virtual void Set<T>(string name, T value, int? expiresInDays = 90)
    {
        // TODO: Change this to sync if/when Blazor supports it
        _ = _jsRuntime.InvokeVoidAsync("CookieInterop.setCookie", name, value, expiresInDays);
    }

    public virtual async Task<T> GetAsync<T>(string name, T defaultValue = default)
    {
        try
        {
            // TODO: Change this to sync if/when Blazor supports it
            return await _jsRuntime.InvokeAsync<T>("CookieInterop.getCookie", name) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public virtual void Remove(string name)
    {
        // TODO: Change this to sync if/when Blazor supports it
        _ = _jsRuntime.InvokeAsync<string>("CookieInterop.removeCookie", name);
    }
}