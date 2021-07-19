using System;
using System.Collections.Generic;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class StringDictionaryExtensions
    {
        public static bool GetFlag<T>(this T dictionary, string key) where T : IDictionary<string, string>
        {
            var value = false;
            if (dictionary.ContainsKey(key) && bool.TryParse(dictionary[key], out value))
            {
                return value;
            }
            return value;
        }

        public static T SetFlag<T>(this T dictionary, string key, bool value) where T : IDictionary<string, string>
        {
            dictionary[key] = value.ToString().ToLower();
            return dictionary;
        }
    }
}
