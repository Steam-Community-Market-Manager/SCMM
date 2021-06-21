using SCMM.Steam.Data.Store.Types;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamStoreItemItemStore
    {
        [Required]
        public Guid StoreId { get; set; }

        public SteamItemStore Store { get; set; }

        [Required]
        public Guid ItemId { get; set; }

        public SteamStoreItem Item { get; set; }

        public int? TopSellerIndex { get; set; }

        /// <summary>
        /// If true, users can submit change requests for this item
        /// </summary>
        public bool IsDraft { get; set; }
    }
}
