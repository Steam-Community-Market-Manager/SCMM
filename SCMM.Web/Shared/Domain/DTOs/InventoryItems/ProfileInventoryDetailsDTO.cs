using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
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

        public DateTimeOffset? LastViewedInventoryOn { get; set; }

        public DateTimeOffset? LastUpdatedInventoryOn { get; set; }

        public IDictionary<string, double> ValueHistoryGraph { get; set; }

        public IDictionary<string, double> ValueProfitGraph { get; set; }
    }
}
