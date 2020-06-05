using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Models.Steam
{
    public class SteamApp : Entity
    {
        public SteamApp()
        {
            Items = new Collection<SteamItem>();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public ICollection<SteamItem> Items { get; set; }
    }
}
