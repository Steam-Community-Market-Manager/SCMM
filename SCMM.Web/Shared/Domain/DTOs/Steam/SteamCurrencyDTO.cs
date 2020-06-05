namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamCurrencyDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string PrefixText { get; set; }

        public string SuffixText { get; set; }
    }
}
