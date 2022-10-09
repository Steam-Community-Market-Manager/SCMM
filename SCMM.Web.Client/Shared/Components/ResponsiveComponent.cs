using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace SCMM.Web.Client.Shared.Components;

public abstract class ResponsiveComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected IBreakpointService BreakpointListener { get; set; }

    protected Breakpoint Breakpoint { get; set; }

    private Guid _breakpointSubscriptionId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var subscriptionResult = await BreakpointListener.Subscribe(
                (breakpoint) =>
                {
                    Breakpoint = breakpoint;
                    OnBreakpointChanged(breakpoint);
                },
                new ResizeOptions
                {
                    ReportRate = 250,
                    NotifyOnBreakpointOnly = true,
                }
            );

            _breakpointSubscriptionId = subscriptionResult.SubscriptionId;

            Breakpoint = subscriptionResult.Breakpoint;
            OnBreakpointChanged(subscriptionResult.Breakpoint);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        if (_breakpointSubscriptionId != Guid.Empty)
        {
            await BreakpointListener.Unsubscribe(_breakpointSubscriptionId);
            _breakpointSubscriptionId = Guid.Empty;
        }
    }

    protected virtual void OnBreakpointChanged(Breakpoint breakpoint)
    {
        InvokeAsync(StateHasChanged);
    }

    protected bool IsMediaSize(Breakpoint breakpoint)
    {
        return BreakpointListener.IsMediaSize(breakpoint, Breakpoint);
    }
}
