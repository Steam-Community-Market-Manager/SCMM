using Skclusive.Core.Component;

namespace SCMM.Web.Client.Shared.Chart
{
    public class DocumentInteropScript : ScriptComponentBase
    {
        protected override string GetScript()
        {
            return @"
                var DocumentInterop = DocumentInterop || {};
                DocumentInterop.setDocumentTitle = function (title) {
                    document.title = title;
                };
            ";
        }
    }
}
