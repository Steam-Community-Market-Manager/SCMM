using Microsoft.AspNetCore.Components;
using Skclusive.Core.Component;
using Skclusive.Material.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Client.Shared.Component.Navigation
{
    public class ErrorComponent : MaterialComponent
    {
        public ErrorComponent() : base("Error")
        {
        }

        [Parameter]
        public string Component { set; get; } = "div";

        [Parameter]
        public string Title { set; get; }

        [Parameter]
        public string SubTitle { set; get; }

        [Parameter]
        public RenderFragment MessageContent { get; set; }

        [Parameter]
        public string ContentStyle { set; get; }

        [Parameter]
        public string ContentClass { set; get; }

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
    }
}
