using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;

namespace SCMM.Shared.Data.Store.Extensions
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

        public static async Task<PaginatedResult<T>> PaginateAsync<T>(this IQueryable<T> query, int start, int count)
        {
            start = Math.Max(0, start);
            count = Math.Max(1, count);
            var data = await query.Skip(start).Take(count).ToArrayAsync();
            var total = await query.CountAsync();
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
        public static async Task<PaginatedResult<T2>> PaginateAsync<T1, T2>(this IQueryable<T1> query, int start, int count, Func<T1, T2> mapper)
        {
            start = Math.Max(0, start);
            count = Math.Max(1, count);
            var data = await query.Skip(start).Take(count).ToArrayAsync();
            var total = await query.CountAsync();
            return new PaginatedResult<T2>()
            {
                Start = start,
                Count = count,
                Total = total,
                Items = data?.Select(x => mapper(x))?.ToArray()
            };
        }
    }
}
