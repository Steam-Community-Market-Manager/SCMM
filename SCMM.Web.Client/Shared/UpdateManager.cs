using Microsoft.JSInterop;
using SCMM.Web.Client;

public class UpdateManager
{
    private readonly IJSRuntime _jsRuntime;

    public UpdateManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SetCallbackAsync(DotNetObjectReference<App> reference)
    {
        await _jsRuntime.InvokeVoidAsync("UpdateInterop.setCallback", reference);
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