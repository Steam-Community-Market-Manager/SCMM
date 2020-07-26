using AutoMapper;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IList<T2> Map<T1, T2>(this IMapper mapper, IQueryable<T1> query, HttpRequest request)
        {
            return query.ToList()
                .Select(x => mapper.Map<T1, T2>(x, opt => opt.AddRequest(request)))
                .ToList();
        }

        public static T2 Map<T1, T2>(this IMapper mapper, T1 obj, HttpRequest request)
        {
            return mapper.Map<T1, T2>(obj, opt => opt.AddRequest(request));
        }
    }
}
