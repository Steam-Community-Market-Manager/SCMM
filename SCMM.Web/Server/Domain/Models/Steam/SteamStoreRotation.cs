using SCMM.Web.Server.Data.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamStoreRotation : Entity
    {
        public SteamStoreRotation()
        {
            Items = new Collection<SteamStoreItem>();
            Media = new PersistableStringCollection();
        }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        public ICollection<SteamStoreItem> Items { get; set; }

        public PersistableStringCollection Media { get; set; }
    }
}
