using SCMM.Shared.Data.Models.Extensions;
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
                case Sort.Ascending: return source.OrderBy(sortBy.AsPropertyNameLambda<T>());
                case Sort.Descending: return source.OrderByDescending(sortBy.AsPropertyNameLambda<T>());
                default: return source;
            }
        }
    }
}
