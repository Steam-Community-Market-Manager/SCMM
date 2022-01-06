using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using System.Collections.ObjectModel;

namespace SCMM.Shared.Data.Store
{
    public abstract class ConfigurableEntity<T> : Entity where T : Configuration, new()
    {
        private static readonly char[] ValueSeparators = new[] { ' ', ',', '+', '&', '|', ';' };

        public ConfigurableEntity()
        {
            Configurations = new Collection<T>();
        }

        protected abstract IEnumerable<ConfigurationDefinition> ConfigurationDefinitions { get; }

        public ICollection<T> Configurations { get; set; }

        private ConfigurationDefinition AssertValidConfiguration(string name)
        {
            var definition = ConfigurationDefinitions.Closest(x => x.Name, name, maxDistance: 3);
            if (string.IsNullOrEmpty(definition.Name))
            {
                throw new ArgumentException($"{name} is not a valid configuration");
            }

            return definition;
        }

        private string[] AssertValidConfigurationValue(string name, params string[] values)
        {
            var definition = ConfigurationDefinitions.Closest(x => x.Name, name, maxDistance: 3);
            if (values?.Length > 0 && definition.AllowMultipleValues)
            {
                values = values.SelectMany(x => x.Split(ValueSeparators, StringSplitOptions.RemoveEmptyEntries)).ToArray();
            }
            if (values?.Length > 1 && !definition.AllowMultipleValues)
            {
                throw new ArgumentException($"{name} only accepts a single option, but you've supplied {values.Length}");
            }
            if (values?.Length > 0 && definition.AllowedValues?.Length > 0)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    var value = definition.AllowedValues.Closest(x => x, values[i]);
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new ArgumentException($"`{values[i]}` is not a valid option for {name}. Valid options are: {String.Join(", ", $"'{definition.AllowMultipleValues}`")}.");
                    }
                    else
                    {
                        values[i] = value;
                    }
                }
            }

            return values?.Where(x => !string.IsNullOrEmpty(x))?.Distinct()?.ToArray() ?? new string[0];
        }

        public KeyValuePair<T, IEnumerable<string>> List(string name)
        {
            var definition = AssertValidConfiguration(name);
            var config = Configurations.FirstOrDefault(x => String.Equals(x.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var result = new List<string>();
            if (config?.List?.Any() == true)
            {
                result.AddRange(config.List);
            }
            else if (!string.IsNullOrEmpty(config?.Value))
            {
                result.Add(config.Value);
            }

            return new KeyValuePair<T, IEnumerable<string>>(config, result);
        }

        public KeyValuePair<T, string> Get(string name, string defaultValue = null)
        {
            var definition = AssertValidConfiguration(name);
            var config = Configurations.FirstOrDefault(x => String.Equals(x.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var result = defaultValue;
            if (!string.IsNullOrEmpty(config?.Value))
            {
                result = config?.Value;
            }
            else if (config?.List?.Any() == true)
            {
                result = string.Join(", ", config?.List);
            }

            return new KeyValuePair<T, string>(config, result);
        }

        public T Set(string name, string value)
        {
            var definition = AssertValidConfiguration(name);
            var config = Configurations.FirstOrDefault(x => String.Equals(x.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var values = AssertValidConfigurationValue(name, value);

            if (config != null)
            {
                if (values.Any())
                {
                    config.Value = (definition.AllowMultipleValues ? null : values.FirstOrDefault());
                    config.List = new PersistableStringCollection(definition.AllowMultipleValues ? values : null);
                }
                else
                {
                    config.Value = null;
                    config.List = new PersistableStringCollection();
                    Configurations.Remove(config);
                }
            }
            else if (values.Any())
            {
                Configurations.Add(config = new T()
                {
                    Name = name,
                    Value = (definition.AllowMultipleValues ? null : values.FirstOrDefault()),
                    List = new PersistableStringCollection(definition.AllowMultipleValues ? values : null)
                });
            }

            return config;
        }

        public bool IsSet(string name)
        {
            return !string.IsNullOrEmpty(Get(name).Value);
        }
    }
}
