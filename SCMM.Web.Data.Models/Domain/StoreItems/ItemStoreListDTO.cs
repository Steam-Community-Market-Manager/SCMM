using System;

namespace SCMM.Web.Data.Models.Domain.StoreItems
{
    public class ItemStoreListDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }
    }
}
