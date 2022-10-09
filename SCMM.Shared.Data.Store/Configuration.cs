using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Shared.Data.Store
{
    public class Configuration : Configuration<Guid> { }

    public class Configuration<TId> : Entity<TId>, IConfigurationOption
    {
        public Configuration()
        {
            List = new PersistableStringCollection();
        }

        [Required]
        public string Name { get; set; }

        public string Value { get; set; }

        [Required]
        public PersistableStringCollection List { get; private set; }

        ICollection<string> IConfigurationOption.List
        {
            get => this.List;
            set => this.List = new PersistableStringCollection(value);
        }
    }

    public interface IConfigurationOption
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public ICollection<string> List { get; set; }
    }
}
