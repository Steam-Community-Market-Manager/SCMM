﻿using Skclusive.Core.Component;

namespace SCMM.Web.Client.Shared.Script
{
    public class WindowInteropScript : ScriptComponentBase
    {
        protected override string GetScript()
        {
            return @"
                var WindowInterop = WindowInterop || {};
                WindowInterop.open = function (url) {
                    window.location.href = url;
                };
                WindowInterop.openInNewTab = function (url) {
                    window.open(url, '_blank');
                };
            ";
        }
    }
}