using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Shared.Data.Store
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

        [Required]
        public PersistableStringCollection List { get; set; }
    }
}
