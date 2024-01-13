﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Profile;

namespace SCMM.Web.Server.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IList<T2> Map<T1, T2>(this IMapper mapper, IEnumerable<T1> query, ControllerBase controller, MyProfileDTO profile = null)
        {
            return query.ToList()
                .Select(x => mapper.Map<T1, T2>(x, opt => opt.AddContext(controller, profile)))
                .ToList();
        }

        public static T2 Map<T1, T2>(this IMapper mapper, T1 obj, ControllerBase controller, MyProfileDTO profile = null)
        {
            return mapper.Map<T1, T2>(obj, opt => opt.AddContext(controller, profile));
        }

        private static IMappingOperationOptions AddContext(this IMappingOperationOptions opt, ControllerBase controller, MyProfileDTO profile = null)
        {
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyUser] = controller.User;
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyLanguage] = controller.Language();
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyCurrency] = controller.Currency();
            opt.Items[AutoMapperConfigurationExtensions.ContextKeyApp] = controller.App();
            if (profile != null)
            {
                opt.Items[AutoMapperConfigurationExtensions.ContextKeyProfile] = profile;
            }
            return opt;
        }
    }
}
