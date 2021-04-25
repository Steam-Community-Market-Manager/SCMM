using System;

namespace SCMM.Steam.Data.Models.Domain.Currencies
{
    public class CurrencyListDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }
    }
}
