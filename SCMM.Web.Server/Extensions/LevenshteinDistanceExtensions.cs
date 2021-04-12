using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.Extensions
{
    public static class LevenshteinDistanceExtensions
    {
        public static T Closest<T>(this IEnumerable<T> options, Func<T, string> predicate, string input, int? maxDistance = null)
        {
            return options
                .Distinct()
                .ToDictionary(x => x, x => predicate(x).ToLower().LevenshteinDistanceFrom(input.ToLower()))
                .Where(x => maxDistance == null || maxDistance <= 0 || x.Value <= maxDistance)
                .OrderBy(x => x.Value)
                .FirstOrDefault()
                .Key;
        }

        public static int LevenshteinDistanceFrom(this string a, string b)
        {
            // https://www.dotnetperls.com/levenshtein
            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
