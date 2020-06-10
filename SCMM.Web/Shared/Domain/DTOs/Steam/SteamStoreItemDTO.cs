using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamStoreItemDTO : SteamItemDTO
    {
        public SteamCurrencyDTO Currency { get; set; }

        public int StorePrice { get; set; }

        public DateTimeOffset? FirstReleasedOn { get; set; }
    }
}
