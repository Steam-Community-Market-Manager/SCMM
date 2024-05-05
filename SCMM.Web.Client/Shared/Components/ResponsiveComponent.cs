using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace SCMM.Web.Client.Shared.Components;

public abstract class ResponsiveComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected IBrowserViewportService BrowserViewportService { get; set; }

    protected Breakpoint Breakpoint { get; set; }

    private Guid _breakpointSubscriptionId = Guid.NewGuid();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(
                _breakpointSubscriptionId,
                (e) =>
                {
                    Breakpoint = e.Breakpoint;
                    OnBreakpointChanged(e.Breakpoint);
                },
                new ResizeOptions
                {
                    ReportRate = 250,
                    NotifyOnBreakpointOnly = true,
                },
                fireImmediately: true
            );
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        if (_breakpointSubscriptionId != Guid.Empty)
        {
            await BrowserViewportService.UnsubscribeAsync(_breakpointSubscriptionId);
            _breakpointSubscriptionId = Guid.Empty;
        }
    }

    protected virtual void OnBreakpointChanged(Breakpoint breakpoint)
    {
        InvokeAsync(StateHasChanged);
    }

    protected bool IsMediaSize(Breakpoint breakpoint)
    {
        return breakpoint switch
        {
            Breakpoint.None => false,
            Breakpoint.Always => true,
            Breakpoint.Xs => breakpoint == Breakpoint.Xs,
            Breakpoint.Sm => breakpoint == Breakpoint.Sm,
            Breakpoint.Md => breakpoint == Breakpoint.Md,
            Breakpoint.Lg => breakpoint == Breakpoint.Lg,
            Breakpoint.Xl => breakpoint == Breakpoint.Xl,
            Breakpoint.Xxl => breakpoint == Breakpoint.Xxl,
            // * and down
            Breakpoint.SmAndDown => breakpoint <= Breakpoint.Sm,
            Breakpoint.MdAndDown => breakpoint <= Breakpoint.Md,
            Breakpoint.LgAndDown => breakpoint <= Breakpoint.Lg,
            Breakpoint.XlAndDown => breakpoint <= Breakpoint.Xl,
            // * and up
            Breakpoint.SmAndUp => breakpoint >= Breakpoint.Sm,
            Breakpoint.MdAndUp => breakpoint >= Breakpoint.Md,
            Breakpoint.LgAndUp => breakpoint >= Breakpoint.Lg,
            Breakpoint.XlAndUp => breakpoint >= Breakpoint.Xl,
            _ => false
        };
    }
}
