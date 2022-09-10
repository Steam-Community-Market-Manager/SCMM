using SCMM.Shared.Data.Models;

namespace SCMM.Web.Data.Models.UI.Currency
{
    public class CurrencyDTO : ICurrency
    {
        public uint Id { get; set; }

        public string Name { get; set; }

        public string PrefixText { get; set; }

        public string SuffixText { get; set; }

        public string CultureName { get; set; }

        public int Scale { get; set; }
    }
}
