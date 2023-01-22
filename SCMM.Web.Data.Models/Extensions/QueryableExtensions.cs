using SCMM.Shared.Data.Models.Extensions;
using System.Linq.Expressions;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, string sortBy, SortDirection sortDirection)
        {
            return source.SortBy(sortBy?.AsPropertyNameLambda<T>(), sortDirection);
        }

        public static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, Expression<Func<T, object>> sortBy, SortDirection sortDirection)
        {
            if (sortBy == null)
            {
                throw new ArgumentNullException(nameof(sortDirection), "Sort property is missing");
            }
            switch (sortDirection)
            {
                case SortDirection.Ascending: return source.OrderBy(sortBy);
                case SortDirection.Descending: return source.OrderByDescending(sortBy);
                default: throw new ArgumentException(nameof(sortDirection), "Sort direction is not supported");
            }
        }

        public static IOrderedQueryable<TSource> OrderByDirection<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, SortDirection direction)
        {
            if (direction == SortDirection.Descending)
            {
                return source.OrderByDescending(keySelector);
            }

            return source.OrderBy(keySelector);
        }
    }
}
