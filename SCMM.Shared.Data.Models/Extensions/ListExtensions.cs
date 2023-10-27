
namespace SCMM.Shared.Data.Models.Extensions
{
    public static class ListExtensions
    {
        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            if (destination is List<T> list)
            {
                list.AddRange(source);
            }
            else
            {
                foreach (var item in source.ToArray())
                {
                    destination.Add(item);
                }
            }
        }

        public static void RemoveAll<T>(this ICollection<T> destination, Predicate<T> match)
        {
            if (destination is List<T> list)
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
                bucket ??= new TSource[size];
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
