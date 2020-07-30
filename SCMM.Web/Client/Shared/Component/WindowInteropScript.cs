using Skclusive.Core.Component;

namespace SCMM.Web.Client.Shared.Component
{
    public class WindowInteropScript : ScriptComponentBase
    {
        protected override string GetScript()
        {
            return @"
                var WindowInterop = WindowInterop || {};
                WindowInterop.openInNewTab = function (url) {
                    window.open(url, '_blank');
                };
            ";
        }
    }
}
