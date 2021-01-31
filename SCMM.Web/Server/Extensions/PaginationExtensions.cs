using SCMM.Web.Shared.Data.Models.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.Extensions
{
    public static class PaginationExtensions
    {
        public static PaginatedResult<T> Paginate<T>(this IQueryable<T> query, int start, int count)
        {
            start = Math.Max(0, start);
            count = Math.Max(1, count);
            var data = query.Skip(start).Take(count).ToArray();
            var total = query.Count();
            return new PaginatedResult<T>()
            {
                Start = start,
                Count = count,
                Total = total,
                Items = data
            };
        }

        public static PaginatedResult<T2> Paginate<T1, T2>(this IQueryable<T1> query, int start, int count, Func<T1, T2> mapper)
        {
            start = Math.Max(0, start);
            count = Math.Max(1, count);
            var data = query.Skip(start).Take(count).ToArray();
            var total = query.Count();
            return new PaginatedResult<T2>()
            {
                Start = start,
                Count = count,
                Total = total,
                Items = data?.Select(x => mapper(x))?.ToArray()
            };
        }
        public static PaginatedResult<T2> Paginate<T1, T2>(this IQueryable<T1> query, int start, int count, Func<IEnumerable<T1>, IEnumerable<T2>> mapper)
        {
            start = Math.Max(0, start);
            count = Math.Max(1, count);
            var data = query.Skip(start).Take(count).ToArray();
            var total = query.Count();
            return new PaginatedResult<T2>()
            {
                Start = start,
                Count = count,
                Total = total,
                Items = mapper(data)?.ToArray()
            };
        }
    }
}
