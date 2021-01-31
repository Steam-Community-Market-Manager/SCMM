using SCMM.Web.Shared.Data.Models.UI;
using System;

namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryActivityDTO : IFilterableItem
    {
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }
    }
}
