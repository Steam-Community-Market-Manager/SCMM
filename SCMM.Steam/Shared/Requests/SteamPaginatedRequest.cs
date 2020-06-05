namespace SCMM.Steam.Shared.Requests
{
    public abstract class SteamPaginatedRequest : SteamRequest
    {
        public int Start { get; set; }

        public int Count { get; set; }

        public string Language { get; set; }

        public string CurrencyId { get; set; }

        public bool NoRender { get; set; } = true;
    }
}
