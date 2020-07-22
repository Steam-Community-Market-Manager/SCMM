using System;

namespace SCMM.Web.Shared.Domain.DTOs.Currencies
{
    public class CurrencyListDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }

        public string Symbol { get; set; }
    }
}
