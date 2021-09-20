using Microsoft.JSInterop;
using SCMM.Shared.Data.Models.Extensions;

public class HttpCookieManager : CookieManager
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCookieManager(IHttpContextAccessor accessor, IJSRuntime jsRuntime) : base(jsRuntime)
    {
        _accessor = accessor;
    }

    public override async Task<T> GetAsync<T>(string name, T defaultValue = default(T))
    {
        var cookie = _accessor.HttpContext.Request.Cookies.FirstOrDefault(x => String.Equals(x.Key, name, StringComparison.InvariantCultureIgnoreCase));
        if (!String.IsNullOrEmpty(cookie.Value))
        {
            return cookie.Value.As<T>() ?? defaultValue;
        }
        else
        {
            return defaultValue;
        }
    }
}