namespace SCMM.Steam.Data.Models.Store.Requests.Json
{
    public abstract class SteamStorePaginatedJsonRequest : SteamRequest
    {
        public int Start { get; set; }

        public int Count { get; set; }

        public string Language { get; set; } = Constants.SteamDefaultLanguage;

        public string CurrencyId { get; set; } = Constants.SteamDefaultCurrencyId.ToString();

        public bool NoRender { get; set; } = true;
    }
}
