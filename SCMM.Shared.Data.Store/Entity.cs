using SCMM.Shared.Abstractions.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Store
{
    public abstract class Entity : Entity<Guid> { }

    public abstract class Entity<TId> : IEventAwareEntity
    {
        private readonly ICollection<IMessage> _eventsToRaise;

        public Entity()
        {
            _eventsToRaise = new Collection<IMessage>();
        }

        [Key]
        [Required]
        public TId Id { get; set; }

        [JsonIgnore]
        [NotMapped]
        public bool IsTransient => Object.Equals(Id, default(TId));

        [JsonIgnore]
        [NotMapped]
        public IEnumerable<IMessage> RaisedEvents => _eventsToRaise;

        public void RaiseEvent(IMessage message)
        {
            _eventsToRaise.Add(message);
        }

        public void ClearEvents()
        {
            _eventsToRaise.Clear();
        }
    }
}
