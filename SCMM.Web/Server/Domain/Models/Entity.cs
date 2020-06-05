using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models
{
    public class Entity
    {
        [Key]
        public Guid Id { get; set; }

        public bool IsTransient => (Id == Guid.Empty);
    }
}
