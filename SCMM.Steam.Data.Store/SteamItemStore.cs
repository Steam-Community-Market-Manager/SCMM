using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
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
            Notes = new PersistableStringCollection();
        }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public string Name { get; set; }

        public DateTimeOffset? Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public ICollection<SteamStoreItemItemStore> Items { get; set; }

        public Guid? ItemsThumbnailId { get; set; }

        public FileData ItemsThumbnail { get; set; }

        [Required]
        public PersistableStringCollection Media { get; set; }

        [Required]
        public PersistableStringCollection Notes { get; set; }

        /// <summary>
        /// If true, users can submit change requests for this store
        /// </summary>
        public bool IsDraft { get; set; }
    }
}
