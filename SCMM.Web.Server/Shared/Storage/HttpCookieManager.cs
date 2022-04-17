using Microsoft.JSInterop;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Web.Client.Shared.Storage;

namespace SCMM.Web.Server.Shared.Storage;

public class HttpCookieManager : CookieManager
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCookieManager(IHttpContextAccessor accessor, IJSRuntime jsRuntime) : base(jsRuntime)
    {
        _accessor = accessor;
    }

    public override Task<T> GetAsync<T>(string name, T defaultValue = default)
    {
        var cookie = _accessor.HttpContext.Request.Cookies.FirstOrDefault(x => String.Equals(x.Key, name, StringComparison.InvariantCultureIgnoreCase));
        if (!String.IsNullOrEmpty(cookie.Value))
        {
            return Task.FromResult(cookie.Value.As<T>() ?? defaultValue);
        }
        else
        {
            return Task.FromResult(defaultValue);
        }
    }
}