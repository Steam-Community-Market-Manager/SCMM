using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SCMM.Shared.Data.Store
{
    public abstract class ConfigurableEntity<T> : Entity where T : Configuration, new()
    {
        public ConfigurableEntity()
        {
            Configurations = new Collection<T>();
        }

        protected abstract IEnumerable<ConfigurationDefinition> ConfigurationDefinitions { get; }

        public ICollection<T> Configurations { get; set; }

        private string AssertValidConfigurationName(string name)
        {
            var definition = ConfigurationDefinitions.Closest(x => x.Name, name, maxDistance: 3);
            if (string.IsNullOrEmpty(definition.Name))
            {
                throw new ArgumentException($"'{name}' is not a valid configuration name");
            }

            return definition.Name;
        }

        private string[] AssertValidConfigurationValue(string name, params string[] values)
        {
            var definition = ConfigurationDefinitions.Closest(x => x.Name, name, maxDistance: 3);
            if (definition.AllowedValues?.Length > 0 && values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var value = definition.AllowedValues.Closest(x => x, values[i]);
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new ArgumentException($"'{values[i]}' is not a valid option for '{name}'");
                    }
                    else
                    {
                        values[i] = value;
                    }
                }
                values = values.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }

            return values;
        }

        public KeyValuePair<T, string> Get(string name, string defaultValue = null)
        {
            name = AssertValidConfigurationName(name);
            var config = Configurations.Closest(x => x.Name, name, maxDistance: 3);
            var result = String.Empty;
            if (!String.IsNullOrEmpty(config?.Value))
            {
                result = config?.Value;
            }
            else if (config?.List?.Any() == true)
            {
                result = String.Join(", ", config?.List);
            }
            else
            {
                result = defaultValue;
            }

            return new KeyValuePair<T, string>(config, result);
        }

        public bool IsSet(string name)
        {
            return !string.IsNullOrEmpty(Get(name).Value);
        }

        public T Set(string name, string value)
        {
            name = AssertValidConfigurationName(name);
            value = AssertValidConfigurationValue(name, value).FirstOrDefault();

            var config = Configurations.Closest(x => x.Name, name, maxDistance: 3);
            if (config != null)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    config.Value = value;
                }
                else
                {
                    config.Value = null;
                    Configurations.Remove(config);
                }
            }
            else if (!string.IsNullOrEmpty(value))
            {
                Configurations.Add(config = new T()
                {
                    Name = name,
                    Value = value
                });
            }

            return config;
        }

        public T Add(string name, params string[] values)
        {
            name = AssertValidConfigurationName(name);
            values = AssertValidConfigurationValue(name, values);
            if (values == null)
            {
                return null;
            }

            var config = Configurations.Closest(x => x.Name, name, maxDistance: 3);
            if (config != null)
            {
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!config.List.Contains(value))
                        {
                            config.List.Add(value);
                        }
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

        public T Remove(string name, params string[] values)
        {
            name = AssertValidConfigurationName(name);
            values = AssertValidConfigurationValue(name, values);
            if (values == null)
            {
                return null;
            }

            var config = Configurations.Closest(x => x.Name, name, maxDistance: 3);
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

        public KeyValuePair<T, IEnumerable<string>> List(string name)
        {
            name = AssertValidConfigurationName(name);
            var config = Configurations.Closest(x => x.Name, name, maxDistance: 3);
            return new KeyValuePair<T, IEnumerable<string>>(
                config,
                config?.List
            );
        }

        public T Clear(string name)
        {
            name = AssertValidConfigurationName(name);
            var config = Configurations.Closest(x => x.Name, name, maxDistance: 3);
            if (config != null)
            {
                Configurations.Remove(config);
            }

            return config;
        }
    }
}
