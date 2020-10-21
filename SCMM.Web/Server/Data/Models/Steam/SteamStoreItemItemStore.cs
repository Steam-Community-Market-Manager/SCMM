using SCMM.Web.Server.Data.Types;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamStoreItemItemStore
    {
        public SteamStoreItemItemStore()
        {
            IndexGraph = new PersistableGraphDataSet();
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
        public PersistableGraphDataSet IndexGraph { get; set; }

    }
}
