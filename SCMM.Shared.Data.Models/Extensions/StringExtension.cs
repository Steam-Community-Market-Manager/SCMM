namespace SCMM.Shared.Data.Models.Extensions
{
    public static class StringExtension
    {
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

        public static string Pluralise(this string value, int count = 0)
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
            else
            {
                return count == 1
                    ? value
                    : $"{value}s";
            }
        }

        public static T As<T>(this string value)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(T));
            if (underlyingType != null && value == null)
            {
                return default(T);
            }
            var baseType = (underlyingType == null ? typeof(T) : underlyingType);
            if (baseType.IsEnum)
            {
                return ((T)Enum.Parse(baseType, value));
            }
            else if (baseType.IsPrimitive)
            {
                return ((T)Convert.ChangeType(value, baseType));
            }
            else
            {
                return default(T);
            }
        }
    }
}
