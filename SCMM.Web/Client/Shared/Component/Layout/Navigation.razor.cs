using Microsoft.AspNetCore.Components;
using Skclusive.Core.Component;
using Skclusive.Material.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Client.Shared.Component.Layout
{
    public class NavigationComponent : MaterialComponent
    {
        public NavigationComponent() : base("Navigation")
        {
        }

        [Parameter]
        public IEnumerable<NavigationItem> Items { set; get; } = Enumerable.Empty<NavigationItem>();

        [Parameter]
        public string ItemStyle { set; get; }

        [Parameter]
        public string ItemClass { set; get; }

        [Parameter]
        public string LinkStyle { set; get; }

        [Parameter]
        public string LinkClass { set; get; }

        [Parameter]
        public string IconStyle { set; get; }

        [Parameter]
        public string IconClass { set; get; }

        [Parameter]
        public string ActiveClass { set; get; }

        protected virtual string _ItemStyle
        {
            get => CssUtil.ToStyle(ItemStyles, ItemStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> ItemStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _ItemClass
        {
            get => CssUtil.ToClass(Selector, ItemClasses, ItemClass);
        }

        protected virtual IEnumerable<string> ItemClasses
        {
            get
            {
                yield return "Item";
            }
        }

        protected virtual string _LinkStyle
        {
            get => CssUtil.ToStyle(LinkStyles, LinkStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> LinkStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _LinkClass
        {
            get => CssUtil.ToClass(Selector, LinkClasses, LinkClass);
        }

        protected virtual IEnumerable<string> LinkClasses
        {
            get
            {
                yield return "Link";
            }
        }

        protected virtual string _IconStyle
        {
            get => CssUtil.ToStyle(IconStyles, IconStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> IconStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _IconClass
        {
            get => CssUtil.ToClass(Selector, IconClasses, IconClass);
        }

        protected virtual IEnumerable<string> IconClasses
        {
            get
            {
                yield return "Icon";
            }
        }

        protected virtual string _ActiveClass
        {
            get => CssUtil.ToClass(Selector, ActiveClasses, ActiveClass);
        }

        protected virtual IEnumerable<string> ActiveClasses
        {
            get
            {
                yield return "Active";
            }
        }
    }
}
