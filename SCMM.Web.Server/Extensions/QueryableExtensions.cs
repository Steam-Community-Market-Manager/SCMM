using SCMM.Data.Shared.Extensions;
using Skclusive.Core.Component;
using System.Linq;

namespace SCMM.Web.Server.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string sortBy, Sort sortDirection)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return source;
            }
            switch (sortDirection)
            {
                case Sort.Ascending: return source.OrderBy(SCMM.Data.Shared.Extensions.QueryableExtensions.ToLambda<T>(sortBy));
                case Sort.Descending: return source.OrderByDescending(SCMM.Data.Shared.Extensions.QueryableExtensions.ToLambda<T>(sortBy));
                default: return source;
            }
        }
    }
}
