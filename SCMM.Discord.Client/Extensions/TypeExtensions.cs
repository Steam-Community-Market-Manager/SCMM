using Discord;

namespace SCMM.Discord.Client.Extensions;

public static class TypeExtensions
{
    public static ApplicationCommandOptionType ToCommandOptionType(this Type type)
    {
        if (type.IsPrimitive)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return ApplicationCommandOptionType.String;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return ApplicationCommandOptionType.Integer;
                case TypeCode.Boolean:
                    return ApplicationCommandOptionType.Boolean;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return ApplicationCommandOptionType.Number;
                default:
                    return ApplicationCommandOptionType.Number;
            }
        }
        else if (typeof(IUser).IsAssignableFrom(type))
        {
            return ApplicationCommandOptionType.User;
        }
        else if (typeof(IRole).IsAssignableFrom(type))
        {
            return ApplicationCommandOptionType.Role;
        }
        else if (typeof(IGuildChannel).IsAssignableFrom(type))
        {
            return ApplicationCommandOptionType.User;
        }

        return ApplicationCommandOptionType.String;
    }
}
