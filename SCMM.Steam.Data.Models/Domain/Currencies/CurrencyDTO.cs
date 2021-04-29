using SCMM.Shared.Data.Models;

namespace SCMM.Steam.Data.Models.Domain.Currencies
{
    public class CurrencyDTO : ICurrency
    {
        public string PrefixText { get; set; }

        public string SuffixText { get; set; }

        public string CultureName { get; set; }

        public int Scale { get; set; }
    }
}
