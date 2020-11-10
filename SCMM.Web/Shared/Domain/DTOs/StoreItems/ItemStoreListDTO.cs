﻿using System;

namespace SCMM.Web.Shared.Domain.DTOs.StoreItems
{
    public class ItemStoreListDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }
    }
}
