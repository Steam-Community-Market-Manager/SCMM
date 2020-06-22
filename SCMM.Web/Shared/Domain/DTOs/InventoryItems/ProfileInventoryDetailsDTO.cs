using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using System;

namespace SCMM.Web.Shared.Domain.DTOs
{
    public class ProfileInventoryDetailsDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string Country { get; set; }

        public InventoryItemListDTO[] InventoryItems { get; set; }
    }
}
