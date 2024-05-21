using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (maxLength <= 3 || String.IsNullOrEmpty(value))
            {
                return value;
            }
            return (value.Length > maxLength)
                ? value.Substring(0, maxLength - 3) + "..."
                : value;
        }

        public static string ToPlainText(this string value)
        {
            if (value == null)
            {
                return null;
            }

            // Strip all HTML tags
            value = Regex.Replace(value, @"<[^>]*>", string.Empty).Trim();

            // Strip all BB tags
            value = Regex.Replace(value, @"\[[^\]]*\]", string.Empty).Trim();

            return value;
        }

        public static string ToSafeMarkup(this string value)
        {
            if (value == null)
            {
                return null;
            }

            // Strip all HTML tags
            value = Regex.Replace(value, @"<[^>]*>", string.Empty).Trim();

            // Convert BB color tags to HTML
            value = Regex.Replace(value, @"\[color=([^\]]*)\]", @"<span style=""color:$1"">").Trim();
            value = Regex.Replace(value, @"\[\/color\]", @"</span>").Trim();

            // Strip all remaining BB tags
            value = Regex.Replace(value, @"\[[^\]]*\]", string.Empty).Trim();

            return value;
        }

        public static string ToPrettyJson(this string json)
        {
            using (var jsonDocument = JsonDocument.Parse(json))
            {
                return JsonSerializer.Serialize(
                    jsonDocument,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );
            }
        }

        public static string RemoveEmptyMarkup(this string value)
        {
            if (value == null)
            {
                return null;
            }

            string lastValue;
            do
            {
                lastValue = value;
                value = Regex.Replace(value, @"<[^>]*>\s*</[^>]*>", string.Empty).Trim();
            } while (value != lastValue);

            return value;
        }

        public static string IsoCountryCodeToFlagEmoji(this string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            return string.Concat(
                value.ToUpper().Trim().Select(x => char.ConvertFromUtf32(x + 0x1F1A5))
            );
        }

        public static string FirstCharToUpper(this string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.First().ToString().ToUpper() + value.Substring(1);
        }

        public static string FirstCharToLower(this string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.First().ToString().ToLower() + value.Substring(1);
        }

        public static string Pluralise(this string value, long count = 0)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            if (value.EndsWith('s'))
            {
                return count == 1
                    ? value.TrimEnd('s')
                    : value;
            }
            if (value.EndsWith('x'))
            {
                return count == 1
                    ? value
                    : $"{value}es";
            }
            else
            {
                return count == 1
                    ? value
                    : $"{value}s";
            }
        }

        public static string Trim(this string value, params string[] trimStrings)
        {
            foreach (var trimString in trimStrings)
            {
                if (value.StartsWith(trimString))
                {
                    value = value.Substring(trimString.Length);
                }
                if (value.EndsWith(trimString))
                {
                    value = value.Substring(0, value.Length - trimString.Length);
                }
            }
            return value;
        }

        public static string MaskIpAddress(this string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }

            var parts = value.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return $"{parts.FirstOrDefault()}.{parts.Skip(1).FirstOrDefault()}.***.***";
        }

        public static T As<T>(this string value)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(T));
            if (underlyingType != null && value == null)
            {
                return default;
            }
            var baseType = (underlyingType == null ? typeof(T) : underlyingType);
            if (baseType.IsEnum)
            {
                return ((T)Enum.Parse(baseType, value, true));
            }
            else
            {
                return ((T)Convert.ChangeType(value, baseType)) ?? default;
            }
        }
    }
}
