﻿using SCMM.Shared.Data.Models;
using System;

namespace SCMM.Web.Data.Models.Domain.Currencies
{
    public class CurrencyDetailedDTO : CurrencyDTO, IExchangeableCurrency
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}