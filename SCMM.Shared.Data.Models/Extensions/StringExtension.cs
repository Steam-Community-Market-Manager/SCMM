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

        public static string Pluralise(this string value, int count)
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
    }
}
