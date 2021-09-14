using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace SCMM.Web.Client.Shared.Dialogs;

public abstract class ResponsiveComponent : ComponentBase
{
    [Inject]
    protected IResizeListenerService ResizeListener { get; set; }

    protected Breakpoint Breakpoint { get; set; } 

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ResizeListener.OnBreakpointChanged += OnResized;
            OnResized(this, await ResizeListener.GetBreakpoint());
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected virtual void OnBreakpointChanged(Breakpoint breakpoint)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnResized(object sender, Breakpoint breakpoint)
    {
        Breakpoint = breakpoint;
        OnBreakpointChanged(breakpoint);
    }

    protected bool IsMediaSize(Breakpoint breakpoint)
    {
        return ResizeListener.IsMediaSize(breakpoint, Breakpoint);
    }

    public void Dispose()
    {
        ResizeListener.OnBreakpointChanged -= OnResized;
    }
}
