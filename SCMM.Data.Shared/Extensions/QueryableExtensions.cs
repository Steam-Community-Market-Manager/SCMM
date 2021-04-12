using System;
using System.Linq;
using System.Linq.Expressions;

namespace SCMM.Data.Shared.Extensions
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderBy(ToLambda<T>(propertyName));
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderByDescending(ToLambda<T>(propertyName));
        }

        public static Expression<Func<T, object>> ToLambda<T>(string propertyName)
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
