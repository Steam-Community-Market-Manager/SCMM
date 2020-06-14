using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamAssetWorkshopFileDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public SteamProfileDTO Creator { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset AcceptedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public int Subscriptions { get; set; }

        public IDictionary<string, double> SubscriptionsGraph { get; set; }

        public int Favourited { get; set; }

        public int Views { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
