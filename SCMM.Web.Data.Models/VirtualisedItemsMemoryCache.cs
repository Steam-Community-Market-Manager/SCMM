using Microsoft.Extensions.Caching.Memory;
using SCMM.Shared.Data.Models;

namespace SCMM.Web.Data.Models;

public class VirtualisedItemsMemoryCache
{
    private IMemoryCache _cache;
    private int _total;

    public VirtualisedItemsMemoryCache()
    {
        Clear();
    }

    public void Clear()
    {
        _total = 0;
        _cache?.Dispose();
        _cache = new MemoryCache(new MemoryCacheOptions()
        {
            TrackStatistics = false,
            TrackLinkedCacheEntries = false,
            SizeLimit = Size
        });
    }

    public async Task<PaginatedResult<T>> Get<T>(int startIndex, int count, Func<Task<PaginatedResult<T>>> loadRemoteDataTask)
    {
        var data = new List<T>();
        var dataIsMissing = false;
        for (int i = startIndex; i <= startIndex + count; i++)
        {
            var found = _cache.TryGetValue(i, out T value);
            if (!found)
            {
                dataIsMissing = true;
                break;
            }
            else
            {
                data.Add(value);
            }
        }

        if (dataIsMissing)
        {
            var remoteData = await loadRemoteDataTask();
            if (remoteData != null)
            {
                if (_total != remoteData.Total)
                {
                    Clear();
                }
                for (int i = 0; i < remoteData.Items.Length; i++)
                {
                    _cache.Set(remoteData.Start + i, remoteData.Items[i], new MemoryCacheEntryOptions()
                    {
                        Size = 1
                    });
                }
            }
            _total = remoteData?.Total ?? 0;
            return remoteData;
        }
        else
        {
            return new PaginatedResult<T>()
            {
                Start = startIndex,
                Count = count,
                Items = data.ToArray(),
                Total = _total
            };
        }
    }

    public int Size { get; set; } = 1000;

    public int Total => _total;
}
