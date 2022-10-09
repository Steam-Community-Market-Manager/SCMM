namespace SCMM.Shared.Abstractions.Finance;

public interface ICurrencyExchangeService
{
    Task<IDictionary<string, decimal>> GetHistoricalExchangeRatesAsync(DateTime date, string from, params string[] to);
}
