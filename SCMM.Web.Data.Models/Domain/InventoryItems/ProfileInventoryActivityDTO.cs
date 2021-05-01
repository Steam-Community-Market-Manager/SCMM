using SCMM.Web.Data.Models.UI;
using System;
using SCMM.Web.Data.Models.Domain.InventoryItems;

namespace SCMM.Web.Data.Models.Domain.InventoryItems
{
    public class ProfileInventoryActivityDTO : IFilterableItem
    {
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }
    }
}
