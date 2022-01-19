namespace SCMM.Shared.Data.Models
{
    public interface ICurrency
    {
        public uint Id { get; }

        public string Name { get; set; }

        public string PrefixText { get; set; }

        public string SuffixText { get; set; }

        public string CultureName { get; set; }

        public int Scale { get; set; }
    }

    public interface IExchangeableCurrency : ICurrency
    {
        public decimal ExchangeRateMultiplier { get; set; }
    }
}
