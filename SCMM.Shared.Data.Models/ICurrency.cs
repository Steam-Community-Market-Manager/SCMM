namespace SCMM.Shared.Data.Models
{
    public interface ICurrency
    {
        public string PrefixText { get; set; }

        public string SuffixText { get; set; }

        public string CultureName { get; set; }

        public int Scale { get; set; }
    }

    public interface IExchangeableCurrency
    {
        public decimal ExchangeRateMultiplier { get; set; }
    }
}
