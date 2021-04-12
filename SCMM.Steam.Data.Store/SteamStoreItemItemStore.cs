using SCMM.Steam.Data.Store.Types;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamStoreItemItemStore
    {
        public SteamStoreItemItemStore()
        {
            IndexGraph = new PersistableHourlyGraphDataSet();
        }

        [Required]
        public Guid ItemId { get; set; }

        public SteamStoreItem Item { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        public SteamItemStore Store { get; set; }

        /// <summary>
        /// Used to track the current sales position in the store
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Used to track the historic sales positions in the store
        /// </summary>
        public PersistableHourlyGraphDataSet IndexGraph { get; set; }

    }
}
