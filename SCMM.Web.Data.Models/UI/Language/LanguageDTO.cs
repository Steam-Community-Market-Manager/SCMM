using SCMM.Shared.Data.Models;

namespace SCMM.Web.Data.Models.UI.Language
{
    public class LanguageDTO : ILanguage
    {
        public string Name { get; set; }

        public string CultureName { get; set; }
    }
}
