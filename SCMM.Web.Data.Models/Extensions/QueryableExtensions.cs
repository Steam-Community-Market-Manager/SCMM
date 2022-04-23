using SCMM.Shared.Data.Models.Extensions;
using System.Linq.Expressions;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> SortBy<T>(this IQueryable<T> source, string sortBy, SortDirection sortDirection)
        {
            return source.SortBy(sortBy?.AsPropertyNameLambda<T>(), sortDirection);
        }

        public static IQueryable<T> SortBy<T>(this IQueryable<T> source, Expression<Func<T, object>> sortBy, SortDirection sortDirection)
        {
            if (sortBy == null)
            {
                return source;
            }
            switch (sortDirection)
            {
                case SortDirection.Ascending: return source.OrderBy(sortBy);
                case SortDirection.Descending: return source.OrderByDescending(sortBy);
                default: return source;
            }
        }
    }
}
