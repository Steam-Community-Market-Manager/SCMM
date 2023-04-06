using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .FirstOrDefault()?
                            .GetCustomAttribute<DisplayAttribute>()?
                            .GetName();
        }

        public static bool IsEnabled(this Enum enumValue)
        {
            return enumValue.GetDisplayName() != null &&
                   enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .FirstOrDefault()?
                            .GetCustomAttribute<ObsoleteAttribute>() == null;
        }
    }
}
