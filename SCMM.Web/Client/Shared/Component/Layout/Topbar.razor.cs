using Microsoft.AspNetCore.Components;
using Skclusive.Core.Component;
using Skclusive.Material.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Client.Shared.Component.Layout
{
    public class TopbarComponent : MaterialComponent
    {
        public TopbarComponent() : base("Topbar")
        {
        }

        [Parameter]
        public string GrowStyle { set; get; }

        [Parameter]
        public string GrowClass { set; get; }

        [Parameter]
        public RenderFragment LogoContent { set; get; }

        [Parameter]
        public RenderFragment ActionsContent { set; get; }

        [Parameter]
        public Action OnSidebarToggle { set; get; }

        protected bool HasLogoContent => LogoContent != null;

        protected virtual string _GrowStyle
        {
            get => CssUtil.ToStyle(GrowStyles, GrowStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> GrowStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _GrowClass
        {
            get => CssUtil.ToClass(Selector, GrowClasses, GrowClass);
        }

        protected virtual IEnumerable<string> GrowClasses
        {
            get
            {
                yield return "Grow";
            }
        }
    }
}
