using SCMM.Web.Server.Data.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamItemStore : Entity
    {
        public SteamItemStore()
        {
            Items = new Collection<SteamStoreItemItemStore>();
            Media = new PersistableStringCollection();
        }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public ICollection<SteamStoreItemItemStore> Items { get; set; }

        public PersistableStringCollection Media { get; set; }
    }
}
