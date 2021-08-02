using System;
using System.Collections.Generic;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class StringDictionaryExtensions
    {
        public static bool GetFlag<T>(this T dictionary, string key) where T : IDictionary<string, string>
        {
            var value = false;
            var decimalValue = 0m;
            var integerValue = 0;
            if (dictionary.ContainsKey(key))
            {
                if (bool.TryParse(dictionary[key], out value))
                {
                    return value;
                }
                else if (decimal.TryParse(dictionary[key], out decimalValue))
                {
                    return decimalValue > 0m;
                }
                else if (int.TryParse(dictionary[key], out integerValue))
                {
                    return integerValue > 0;
                }
            }
            return value;
        }

        public static T SetFlag<T>(this T dictionary, string key, bool value) where T : IDictionary<string, string>
        {
            dictionary[key] = value.ToString().ToLower();
            return dictionary;
        }

        public static T SetFlag<T>(this T dictionary, string key, decimal value) where T : IDictionary<string, string>
        {
            dictionary[key] = Math.Round(value, 4).ToString();
            return dictionary;
        }
    }
}
