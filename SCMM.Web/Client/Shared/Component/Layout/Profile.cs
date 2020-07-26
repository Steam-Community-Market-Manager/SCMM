using Microsoft.AspNetCore.Components;
using Skclusive.Core.Component;
using Skclusive.Material.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Client.Shared.Component.Layout
{
    public class ProfileComponent : MaterialComponent
    {
        public ProfileComponent() : base("Profile")
        {
        }

        [Parameter]
        public string Component { set; get; } = "div";

        [Parameter]
        public string Name { set; get; }

        [Parameter]
        public string Avatar { set; get; }

        [Parameter]
        public string SubTitle { set; get; }

        [Parameter]
        public string AvatarStyle { set; get; }

        [Parameter]
        public string AvatarClass { set; get; }

        [Parameter]
        public string NameStyle { set; get; }

        [Parameter]
        public string NameClass { set; get; }

        protected virtual string _AvatarStyle
        {
            get => CssUtil.ToStyle(AvatarStyles, AvatarStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> AvatarStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _AvatarClass
        {
            get => CssUtil.ToClass(Selector, AvatarClasses, AvatarClass);
        }

        protected virtual IEnumerable<string> AvatarClasses
        {
            get
            {
                yield return "Avatar";
            }
        }

        protected virtual string _NameStyle
        {
            get => CssUtil.ToStyle(NameStyles, NameStyle);
        }

        protected virtual IEnumerable<Tuple<string, object>> NameStyles
        {
            get => Enumerable.Empty<Tuple<string, object>>();
        }

        protected virtual string _NameClass
        {
            get => CssUtil.ToClass(Selector, NameClasses, NameClass);
        }

        protected virtual IEnumerable<string> NameClasses
        {
            get
            {
                yield return "Name";
            }
        }
    }
}
