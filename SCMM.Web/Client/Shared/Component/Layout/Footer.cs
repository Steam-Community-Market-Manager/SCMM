using Skclusive.Material.Core;
using Microsoft.AspNetCore.Components;

namespace SCMM.Web.Client.Shared.Component.Layout
{
    public class FooterComponent : MaterialComponent
    {
        public FooterComponent() : base("Footer")
        {
        }

        [Parameter]
        public string Component { set; get; } = "div";

        [Parameter]
        public string Name { set; get; }

        [Parameter]
        public string Copyright { set; get; }
    }
}
