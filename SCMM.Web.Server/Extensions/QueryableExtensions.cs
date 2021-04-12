using Skclusive.Core.Component;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace SCMM.Web.Server.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string sortBy, Sort sortDirection)
        {
            if (String.IsNullOrEmpty(sortBy))
            {
                return source;
            }
            switch (sortDirection)
            {
                case Sort.Ascending: return source.OrderBy(ToLambda<T>(sortBy));
                case Sort.Descending: return source.OrderByDescending(ToLambda<T>(sortBy));
                default: return source;
            }
        }

        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderBy(ToLambda<T>(propertyName));
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderByDescending(ToLambda<T>(propertyName));
        }

        private static Expression<Func<T, object>> ToLambda<T>(string propertyName)
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
