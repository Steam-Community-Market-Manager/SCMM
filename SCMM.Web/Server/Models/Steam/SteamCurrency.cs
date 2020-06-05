using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Models.Steam
{
    public class SteamCurrency : Entity
    {
        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string PrefixText { get; set; }

        public string SuffixText { get; set; }
    }
}
