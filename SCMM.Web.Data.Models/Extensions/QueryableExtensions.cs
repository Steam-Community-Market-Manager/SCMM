using SCMM.Shared.Data.Models.Extensions;
using SCMM.Web.Data.Models.Extensions;
using System.Linq;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string sortBy, SortDirection sortDirection)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return source;
            }
            switch (sortDirection)
            {
                case SortDirection.Ascending: return source.OrderBy(sortBy.AsPropertyNameLambda<T>());
                case SortDirection.Descending: return source.OrderByDescending(sortBy.AsPropertyNameLambda<T>());
                default: return source;
            }
        }
    }
}
