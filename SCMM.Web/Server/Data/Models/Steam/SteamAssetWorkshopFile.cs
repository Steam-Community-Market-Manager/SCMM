using SCMM.Web.Server.Data.Types;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamAssetWorkshopFile : Entity
    {
        public SteamAssetWorkshopFile()
        {
            SubscriptionsGraph = new PersistableGraphDataSet();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public Guid? CreatorId { get; set; }

        public SteamProfile Creator { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public DateTimeOffset? AcceptedOn { get; set; }

        public int Subscriptions { get; set; }

        public PersistableGraphDataSet SubscriptionsGraph { get; set; }

        public int Favourited { get; set; }

        public int Views { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
