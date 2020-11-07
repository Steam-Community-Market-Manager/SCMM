using SCMM.Web.Server.Data.Types;
using SCMM.Web.Shared.Data.Models.Steam;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamAssetWorkshopFile : Entity
    {
        public SteamAssetWorkshopFile()
        {
            SubscriptionsGraph = new PersistableDailyGraphDataSet();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public Guid? CreatorId { get; set; }

        public SteamProfile Creator { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public DateTimeOffset? AcceptedOn { get; set; }

        public int Subscriptions { get; set; }

        public PersistableDailyGraphDataSet SubscriptionsGraph { get; set; }

        public int Favourited { get; set; }

        public int Views { get; set; }

        public SteamAssetWorkshopFileFlags Flags { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
