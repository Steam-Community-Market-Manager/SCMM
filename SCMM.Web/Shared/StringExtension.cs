using System;

namespace SCMM.Web.Shared
{
    public static class StringExtension
    {
        public static string Pluralise(this string value, int count)
        {
            if (value.EndsWith('s'))
            {
                return (count == 1)
                    ? value.TrimEnd('s')
                    : value;
            }
            else
            {
                return (count == 1)
                    ? value
                    : $"{value}s";
            }
        }
    }
}
