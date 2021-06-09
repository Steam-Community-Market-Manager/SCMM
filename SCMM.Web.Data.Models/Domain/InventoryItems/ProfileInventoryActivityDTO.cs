using SCMM.Web.Data.Models.UI;
using System;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.Domain.InventoryItems
{
    public class ProfileInventoryActivityDTO : ISearchable
    {
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public long Movement { get; set; }

        [JsonIgnore]
        public object[] SearchData => new object[] { Name };
    }
}
