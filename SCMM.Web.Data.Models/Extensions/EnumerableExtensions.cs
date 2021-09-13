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
            return source.Where(x => x.Filters?.Any(y => y.Contains(filterBy, StringComparison.InvariantCultureIgnoreCase)) == true);
        }
    }
}
