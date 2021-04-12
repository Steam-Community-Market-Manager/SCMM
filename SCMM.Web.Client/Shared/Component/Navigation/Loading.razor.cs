using Microsoft.AspNetCore.Components;
using Skclusive.Material.Core;

namespace SCMM.Web.Client.Shared.Component.Navigation
{
    public class LoadingComponent : MaterialComponent
    {
        public LoadingComponent() : base("Loading")
        {
        }

        [Parameter]
        public string Component { set; get; } = "div";

        [Parameter]
        public string Message { set; get; } = "Loading...";
    }
}
