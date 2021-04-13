﻿using SCMM.Data.Shared.Store.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Data.Shared.Store
{
    public class Configuration : Entity
    {
        public Configuration()
        {
            List = new PersistableStringCollection();
        }

        [Required]
        public string Name { get; set; }

        public string Value { get; set; }

        public PersistableStringCollection List { get; set; }
    }
}