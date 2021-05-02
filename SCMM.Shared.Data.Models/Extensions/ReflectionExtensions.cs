using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SCMM.Shared.Data.Models.Extensions
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetTypesAssignableTo(this Assembly[] assemblies, Type baseType)
        {
            return assemblies.SelectMany(x => x.GetTypesAssignableTo(baseType)).ToList();
        }

        public static IEnumerable<Type> GetTypesAssignableTo(this Assembly assembly, Type baseType)
        {
            return assembly.ExportedTypes.Where(type => IsAssignableTo(type, baseType)).ToList();
        }

        public static bool IsAssignableTo(this Type type, Type baseType)
        {
            if (type.IsClass)
            {
                if (!baseType.IsAssignableFrom(type))
                {
                    return type.IsAssignableToGenericType(baseType);
                }

                return true;
            }

            return false;
        }

        public static bool IsAssignableToGenericType(this Type type, Type genericType)
        {
            if (!type.GetInterfaces().Any(it => it.GetTypeInfo().IsGenericType && it.GetGenericTypeDefinition() == genericType) && (!type.GetTypeInfo().IsGenericType || !(type.GetGenericTypeDefinition() == genericType)))
            {
                if (type.GetTypeInfo().BaseType != null)
                {
                    return type.GetTypeInfo().BaseType.IsAssignableToGenericType(genericType);
                }

                return false;
            }

            return true;
        }

        public static IEnumerable<Type> GetInterfacesOfGenericType(this Type type, Type genericType)
        {
            return (from it in type.GetInterfaces()
                    where it.GetTypeInfo().IsGenericType && it.GetGenericTypeDefinition() == genericType
                    select it).ToList();
        }
    }
}
