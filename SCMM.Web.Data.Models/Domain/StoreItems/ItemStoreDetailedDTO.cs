using System;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models.Domain.StoreItems
{
    public class ItemStoreDetailedDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public IList<StoreItemDetailDTO> Items { get; set; }

        public IList<string> Media { get; set; }
    }
}
