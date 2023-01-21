using Microsoft.JSInterop;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Web.Client.Services;

namespace SCMM.Web.Server.Services;

public class HttpContextCookieManager : JavascriptCookieManager
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCookieManager(IHttpContextAccessor accessor, IJSRuntime jsRuntime) : base(jsRuntime)
    {
        _accessor = accessor;
    }

    public override Task<T> GetAsync<T>(string name, T defaultValue = default)
    {
        var cookie = _accessor.HttpContext.Request.Cookies.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.InvariantCultureIgnoreCase));
        if (!string.IsNullOrEmpty(cookie.Value))
        {
            return Task.FromResult(cookie.Value.As<T>() ?? defaultValue);
        }
        else
        {
            return Task.FromResult(defaultValue);
        }
    }
}