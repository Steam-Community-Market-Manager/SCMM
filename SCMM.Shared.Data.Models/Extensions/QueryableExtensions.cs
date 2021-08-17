using System.Linq.Expressions;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderBy(propertyName.AsPropertyNameLambda<T>());
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderByDescending(propertyName.AsPropertyNameLambda<T>());
        }

        public static Expression<Func<T, object>> AsPropertyNameLambda<T>(this string propertyName)
        {
            var propertyNames = propertyName.Split('.');
            var parameter = Expression.Parameter(typeof(T));
            Expression body = parameter;
            foreach (var propName in propertyNames)
            {
                body = Expression.Property(body, propName);
            }

            var propertyAsObject = Expression.Convert(body, typeof(object));
            return Expression.Lambda<Func<T, object>>(propertyAsObject, parameter);
        }
    }
}
