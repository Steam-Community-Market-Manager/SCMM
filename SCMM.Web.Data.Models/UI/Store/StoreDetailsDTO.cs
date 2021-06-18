using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreDetailsDTO : ISearchable
    {
        public Guid Guid { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public IList<StoreItemDetailsDTO> Items { get; set; }

        public IList<string> Media { get; set; }

        [JsonIgnore]
        public object[] SearchData => new object[] { Id, Name };
    }
}
