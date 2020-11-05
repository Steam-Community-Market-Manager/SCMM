using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Skclusive.Material.Menu;
using System;
using System.Threading.Tasks;

namespace SCMM.Web.Client.Shared.Component.ContextMenu
{
    public class ContextMenuItemComponent : MenuItemComponent
    {
        [CascadingParameter]
        public ContextContainerComponent ContextContainer { get; set; }

        protected async Task CloseMenuAndHandleClickAsync(EventArgs args)
        {
            ContextContainer?.CloseMenu();
            await HandleClickAsync(args);
        }

        protected async Task CloseMenuAndHandleKeyDownAsync(KeyboardEventArgs args)
        {
            await base.HandleKeyDownAsync(args);

            if (Enter && args.Key == "Enter")
            {
                ContextContainer?.CloseMenu();
                await OnClick.InvokeAsync(args);
            }
        }
    }
}
