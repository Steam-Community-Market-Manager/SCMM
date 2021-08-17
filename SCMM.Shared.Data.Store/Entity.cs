using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Shared.Data.Store
{
    public abstract class Entity
    {
        [Key]
        public Guid Id { get; set; }

        [NotMapped]
        public bool IsTransient => Id == Guid.Empty;
    }
}
