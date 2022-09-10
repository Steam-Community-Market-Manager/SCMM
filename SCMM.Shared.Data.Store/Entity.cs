using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Store
{
    public abstract class Entity : Entity<Guid> { }

    public abstract class Entity<TId>
    {
        [Key]
        [Required]
        public TId Id { get; set; }

        [JsonIgnore]
        [NotMapped]
        public bool IsTransient => Object.Equals(Id, default(TId));
    }
}
