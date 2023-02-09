using SCMM.Web.Data.Models.UI;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> FilterBy<T>(this IEnumerable<T> source, string filterBy)
            where T : ICanBeFiltered
        {
            if (filterBy == null)
            {
                return source;
            }
            var filterWords = filterBy.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return source.Where(x => filterWords.All(word => x.Filters?.Any(f => f?.Contains(word, StringComparison.InvariantCultureIgnoreCase) == true) == true));
        }

        public static TValue Median<TColl, TValue>(this IEnumerable<TColl> source, Func<TColl, TValue> selector)
        {
            return source.Select<TColl, TValue>(selector).Median();
        }

        public static T Median<T>(this IEnumerable<T> source)
        {
            var array = source.ToArray();
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                array = array.Where(x => x != null).ToArray();
            }

            var count = array.Count();
            if (count == 0)
            {
                return default(T);
            }

            var midpoint = (int) Math.Round((decimal)count / 2, 0);
            return array
                .OrderBy(n => n)
                .ElementAt(midpoint);
        }
    }
}
