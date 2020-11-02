using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SCMM.Web.Server.Data.Models
{
    public class ConfigurableEntity<T> : Entity where T : Configuration
    {
        public ConfigurableEntity()
        {
            Configurations = new Collection<T>();
        }

        public ICollection<T> Configurations { get; set; }

        public string Get(string config)
        {
            return Configurations.FirstOrDefault(x => x.Name == config)?.Value;
        }

        public void Set(string config, string value)
        {
            var configToUpdate = Configurations.FirstOrDefault(x => x.Name == config);
            if (configToUpdate != null)
            {
                configToUpdate.Value = value;
            }
        }

        public void Add(string config, string value)
        {
            var list = Configurations.FirstOrDefault(x => x.Name == config)?.List;
            if (list != null)
            {
                list.Add(value);
            }
        }

        public void Remove(string config, string value)
        {
            var list = Configurations.FirstOrDefault(x => x.Name == config)?.List;
            if (list != null)
            {
                list.Remove(value);
            }
        }

        public IEnumerable<string> List(string config)
        {
            return Configurations.FirstOrDefault(x => x.Name == config)?.List;
        }

        public void Clear(string config)
        {
            var configsToRemove = Configurations.Where(x => x.Name == config).ToArray();
            foreach (var x in configsToRemove)
            {
                Configurations.Remove(x);
            }
        }
    }
}
