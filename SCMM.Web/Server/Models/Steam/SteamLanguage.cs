using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Models.Steam
{
    public class SteamLanguage : Entity
    {
        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
