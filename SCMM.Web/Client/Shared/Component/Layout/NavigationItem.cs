using Microsoft.AspNetCore.Components;
using System;

namespace SCMM.Web.Client.Shared.Component.Layout
{
    public class NavigationItem
    {
        public RenderFragment Icon { set; get; }

        public string Title { set; get; }

        public string Path { set; get; }

        public Action OnClick { set; get; }

        public bool Prefix { set; get; }
    }
}
