using System;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamMarketItemActivity : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}
