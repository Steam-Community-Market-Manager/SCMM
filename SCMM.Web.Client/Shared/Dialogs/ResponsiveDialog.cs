using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace SCMM.Web.Client.Shared.Dialogs;

public abstract class ResponsiveDialog : ComponentBase
{
    [CascadingParameter]
    public MudDialogInstance Dialog { get; set; }

    protected ResponsiveDialogOptions Options { get; set; }

    [Inject]
    protected IResizeListenerService ResizeListener { get; set; }

    protected abstract void OnConfigure(ResponsiveDialogOptions options);

    protected override async Task OnInitializedAsync()
    {
        Options = new ResponsiveDialogOptions(Dialog.Options);
        OnConfigure(Options);
        Dialog.SetOptions(Options);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ResizeListener.OnBreakpointChanged += OnResized;
            OnResized(ResizeListener, await ResizeListener.GetBreakpoint());
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void OnResized(object sender, Breakpoint breakpoint)
    {
        Options.FullScreen = (breakpoint <= Options.FullscreenBreakpoint);
        Options.NoHeader = (Options.FullScreen == true ? false : Dialog.Options.NoHeader);
        Options.CloseButton = (Options.FullScreen == true ? true : Dialog.Options.CloseButton);
        Dialog.SetOptions(Options);
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ResizeListener.OnBreakpointChanged -= OnResized;
    }
}
