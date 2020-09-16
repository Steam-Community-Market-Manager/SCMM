using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Shared
{
    public static class ListExtensions
    {
        public static TList AddIfMissing<TList, TElement>(this TList list, TElement element)
            where TList : ICollection<TElement>
        {
            if (!list.Contains(element))
            {
                list.Add(element);
            }
            return list;
        }

        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            TSource[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new TSource[size];
                }

                bucket[count++] = item;
                if (count != size)
                {
                    continue;
                }

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
            {
                yield return bucket.Take(count).ToArray();
            }
        }
    }
}
