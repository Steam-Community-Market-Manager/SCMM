using SCMM.Shared.Data.Models.Extensions;
using SCMM.Web.Data.Models.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace SCMM.Web.Data.Models.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string sortBy, SortDirection sortDirection)
        {
            return source.OrderBy(sortBy?.AsPropertyNameLambda<T>(), sortDirection);
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, Expression<Func<T, object>> sortBy, SortDirection sortDirection)
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
