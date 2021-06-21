using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
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

        public Guid? ItemsThumbnailId { get; set; }

        public ImageData ItemsThumbnail { get; set; }

        public PersistableStringCollection Media { get; set; }

        /// <summary>
        /// If true, this store can be purchased from. Otherwise, it is no longer available.
        /// </summary>
        public bool IsAvailable => (End == null || End < DateTimeOffset.Now);

        /// <summary>
        /// If true, users can submit change requests for this store
        /// </summary>
        public bool IsDraft { get; set; }
    }
}
