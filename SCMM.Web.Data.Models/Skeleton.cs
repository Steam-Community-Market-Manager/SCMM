using System;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models
{
    public static class Skeleton
    {
        public static List<T> List<T>(int min = 5, int max = 10)
        {
            return List<List<T>, T>(min, max);
        }

        public static TList List<TList, T>(int min = 5, int max = 10) where TList : IList<T>, new()
        {
            var list = new TList();
            var count = new Random().Next(min, max);
            for (int i = 0; i < count; i++)
            {
                list.Add(default(T));
            }
            return list;
        }

        public static string Width(double scale = 1.0)
        {
            return $"{Math.Round(new Random().Next(25, 75) * scale, 0)}%";
        }
    }
}
