using SCMM.Web.Server.Data.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SCMM.Web.Server.Data.Models
{
    public class ConfigurableEntity<T> : Entity where T : Configuration, new()
    {
        public ConfigurableEntity()
        {
            Configurations = new Collection<T>();
        }

        public ICollection<T> Configurations { get; set; }

        public string Get(string name)
        {
            var config = Configurations.FirstOrDefault(x => x.Name == name);
            return (config?.Value ?? config?.List?.FirstOrDefault());
        }

        public T Set(string name, string value)
        {
            var config = Configurations.FirstOrDefault(x => x.Name == name);
            if (config != null)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    config.Value = value;
                }
                else
                {
                    config.Value = null;
                    Configurations.Remove(config);
                }
            }
            else if (!String.IsNullOrEmpty(value))
            {
                Configurations.Add(config = new T()
                {
                    Name = name,
                    Value = value
                });
            }

            return config;
        }

        public T Add(string name, string[] values)
        {
            if (values == null)
            {
                return null;
            }
            var config = Configurations.FirstOrDefault(x => x.Name == name);
            if (config != null)
            {
                foreach (var value in values)
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        config.List.Add(value);
                    }
                }
            }
            else
            {
                Configurations.Add(config = new T()
                {
                    Name = name,
                    List = new PersistableStringCollection(values)
                });
            }

            return config;
        }

        public T Remove(string name, string[] values)
        {
            if (values == null)
            {
                return null;
            }
            var config = Configurations.FirstOrDefault(x => x.Name == name);
            if (config != null)
            {
                foreach (var value in values)
                {
                    config.List.Remove(value);
                    if (!config.List.Any())
                    {
                        Configurations.Remove(config);
                    }
                }
            }

            return config;
        }

        public IEnumerable<string> List(string name)
        {
            return Configurations.FirstOrDefault(x => x.Name == name)?.List;
        }

        public T Clear(string name)
        {
            var config = Configurations.FirstOrDefault(x => x.Name == name);
            if (config != null)
            {
                Configurations.Remove(config);
            }

            return config;
        }
    }
}
