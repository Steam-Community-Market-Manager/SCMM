using System;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models.Domain.StoreItems
{
    public class StoreDTO
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public IList<StoreItemDTO> Items { get; set; }

        public IList<string> Media { get; set; }
    }
}
