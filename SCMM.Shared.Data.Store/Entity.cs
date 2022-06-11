using HotChocolate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Store
{
    public abstract class Entity
    {
        [GraphQLIgnore]
        [Key]
        public Guid Id { get; set; }

        [GraphQLIgnore]
        [JsonIgnore]
        [NotMapped]
        public bool IsTransient => Id == Guid.Empty;
    }
}
