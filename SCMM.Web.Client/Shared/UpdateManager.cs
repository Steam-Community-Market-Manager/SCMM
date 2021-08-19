using Microsoft.JSInterop;
using SCMM.Web.Client;

public class UpdateManager
{
    private readonly IJSRuntime _jsRuntime;

    public UpdateManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetInstanceAsync(DotNetObjectReference<App> instance)
    {
        await _jsRuntime.InvokeVoidAsync("UpdateInterop.setInstance", instance);
    }

    public async Task<bool> IsUpdatePendingAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("UpdateInterop.isUpdatePending");
    }

    public async Task ApplyPendingUpdateAsync()
    {
        await _jsRuntime.InvokeVoidAsync("UpdateInterop.applyPendingUpdate");
    }
}