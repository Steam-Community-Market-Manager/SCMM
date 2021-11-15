using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Store
{
    public abstract class Entity
    {
        [Key]
        public Guid Id { get; set; }

        [JsonIgnore]
        [NotMapped]
        public bool IsTransient => Id == Guid.Empty;
    }
}
