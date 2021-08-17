using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace SCMM.Web.Server.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IList<T2> Map<T1, T2>(this IMapper mapper, IEnumerable<T1> query, ControllerBase controller)
        {
            return query.ToList()
                .Select(x => mapper.Map<T1, T2>(x, opt => opt.AddContext(controller)))
                .ToList();
        }

        public static T2 Map<T1, T2>(this IMapper mapper, T1 obj, ControllerBase controller)
        {
            return mapper.Map<T1, T2>(obj, opt => opt.AddContext(controller));
        }

        private static IMappingOperationOptions AddContext(this IMappingOperationOptions opt, ControllerBase controller)
        {
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyUser] = controller.User;
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyLanguage] = controller.Language();
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyCurrency] = controller.Currency();
            return opt;
        }
    }
}
