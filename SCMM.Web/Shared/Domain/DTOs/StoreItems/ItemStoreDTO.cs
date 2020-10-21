using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.StoreItems
{
    public class ItemStoreDTO
    {
        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public IList<StoreItemDetailDTO> Items { get; set; }

        public IList<string> Media { get; set; }
    }
}
