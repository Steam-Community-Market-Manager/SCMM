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
    }
}
