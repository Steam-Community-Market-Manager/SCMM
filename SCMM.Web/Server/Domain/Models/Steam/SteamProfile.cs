using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamProfile : Entity
    {
        public SteamProfile()
        {
            InventoryItems = new Collection<SteamInventoryItem>();
            WorkshopFiles = new Collection<SteamAssetWorkshopFile>();
        }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string Country { get; set; }

        public Guid? LanguageId { get; set; }

        public SteamLanguage Language { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public DateTimeOffset? LastViewedInventoryOn { get; set; }

        public DateTimeOffset? LastUpdatedInventoryOn { get; set; }

        public ICollection<SteamInventoryItem> InventoryItems { get; set; }

        public ICollection<SteamAssetWorkshopFile> WorkshopFiles { get; set; }
    }
}
