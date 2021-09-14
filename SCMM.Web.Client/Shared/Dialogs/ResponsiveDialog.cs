using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SCMM.Web.Client.Shared.Dialogs;

public abstract class ResponsiveDialog : ResponsiveComponent
{
    [CascadingParameter]
    public MudDialogInstance Dialog { get; set; }

    protected ResponsiveDialogOptions Options { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Options = new ResponsiveDialogOptions(Dialog.Options);
        OnConfigure(Options);
        Dialog.SetOptions(Options);
    }

    protected abstract void OnConfigure(ResponsiveDialogOptions options);

    protected override void OnBreakpointChanged(Breakpoint breakpoint)
    {
        Options.FullScreen = (breakpoint <= Options.FullscreenBreakpoint);
        Options.NoHeader = (Options.FullScreen == true ? false : Dialog.Options.NoHeader);
        Options.CloseButton = (Options.FullScreen == true ? true : Dialog.Options.CloseButton);
        Dialog.SetOptions(Options);
        InvokeAsync(StateHasChanged);
    }
}
