namespace SCMM.Shared.Data.Models.Extensions
{
    public static class ListExtensions
    {
        public static void AddIfMissing<T>(this ICollection<T> list, T element)
        {
            if (!list.Contains(element))
            {
                list.Add(element);
            }
        }

        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            var list = destination as List<T>;
            if (list != null)
            {
                list.AddRange(source);
            }
            else
            {
                foreach (var item in source)
                {
                    destination.Add(item);
                }
            }
        }

        public static void RemoveAll<T>(this ICollection<T> destination, Predicate<T> match)
        {
            var list = destination as List<T>;
            if (list != null)
            {
                list.RemoveAll(match);
            }
            else
            {
                var toBeRemoved = destination.Where(x => match.Invoke(x)).ToArray();
                foreach (var item in toBeRemoved)
                {
                    destination.Remove(item);
                }
            }
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
