using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Models
{
    public class Entity
    {
        [Key]
        public Guid Id { get; set; }

        public bool IsTransient => (Id == Guid.Empty);
    }
}
