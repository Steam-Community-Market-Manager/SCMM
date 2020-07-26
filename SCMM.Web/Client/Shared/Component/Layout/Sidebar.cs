using Microsoft.AspNetCore.Components;
using Skclusive.Core.Component;
using Skclusive.Material.Core;
using Skclusive.Material.Drawer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Client.Shared.Component.Layout
{
    public class SidebarComponent : MaterialComponent
    {
        public SidebarComponent() : base("Sidebar")
        {
        }

        [Parameter]
        public bool Open { set; get; }

        [Parameter]
        public DrawerVariant Variant { set; get; }

        [Parameter]
        public Action OnClose { set; get; }

        [Parameter]
        public string ContentStyle { set; get; }

        [Parameter]
        public string ContentClass { set; get; }

        [Parameter]
        public string DrawerStyle { set; get; }

        [Parameter]
        public string DrawerClass { set; get; }

        [Parameter]
        public string NavigationStyle { set; get; }

        [Parameter]
        public string NavigationClass { set; get; }

        [Parameter]
        public string DividerStyle { set; get; }

        [Parameter]
        public string DividerClass { set; get; }

        protected virtual string _ContentStyle
        {
            get => CssUtil.ToStyle(ContentStyles, ContentStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> ContentStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _ContentClass
        {
            get => CssUtil.ToClass(Selector, ContentClasses, ContentClass);
        }

        protected virtual IEnumerable<string> ContentClasses
        {
            get
            {
                yield return "Content";
            }
        }

        protected virtual string _DrawerStyle
        {
            get => CssUtil.ToStyle(DrawerStyles, DrawerStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> DrawerStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _DrawerClass
        {
            get => CssUtil.ToClass(Selector, DrawerClasses, DrawerClass);
        }

        protected virtual IEnumerable<string> DrawerClasses
        {
            get
            {
                yield return "Drawer";
            }
        }

        protected virtual string _NavigationStyle
        {
            get => CssUtil.ToStyle(NavigationStyles, NavigationStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> NavigationStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _NavigationClass
        {
            get => CssUtil.ToClass(Selector, NavigationClasses, NavigationClass);
        }

        protected virtual IEnumerable<string> NavigationClasses
        {
            get
            {
                yield return "Navigation";
            }
        }

        protected virtual string _DividerStyle
        {
            get => CssUtil.ToStyle(DividerStyles, DividerStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> DividerStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _DividerClass
        {
            get => CssUtil.ToClass(Selector, DividerClasses, DividerClass);
        }

        protected virtual IEnumerable<string> DividerClasses
        {
            get
            {
                yield return "Divider";
            }
        }
    }
}
