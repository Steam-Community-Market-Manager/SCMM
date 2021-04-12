﻿using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketPriceOverviewJsonRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string MarketHashName { get; set; }

        public string Language { get; set; }

        public string CurrencyId { get; set; }

        public bool NoRender { get; set; } = true;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/priceoverview?appid={Uri.EscapeDataString(AppId)}&market_hash_name={Uri.EscapeDataString(MarketHashName)}&language={Language}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}