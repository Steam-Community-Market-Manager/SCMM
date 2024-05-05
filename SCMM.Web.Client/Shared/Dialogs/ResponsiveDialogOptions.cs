using MudBlazor;

namespace SCMM.Web.Client.Shared.Dialogs;

public class ResponsiveDialogOptions : DialogOptions
{
    public ResponsiveDialogOptions(DialogOptions options)
    {
        Position = options.Position;
        MaxWidth = options.MaxWidth;
        BackdropClick = options.BackdropClick;
        NoHeader = options.NoHeader;
        CloseButton = options.CloseButton;
        FullScreen = options.FullScreen;
        FullWidth = options.FullWidth;
    }

    public Breakpoint FullscreenBreakpoint { get; set; } = Breakpoint.Sm;
}
