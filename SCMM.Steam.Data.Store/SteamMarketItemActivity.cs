﻿using SCMM.Data.Shared.Store;
using System;

namespace SCMM.Steam.Data.Store
{
    public class SteamMarketItemActivity : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }
    }
}