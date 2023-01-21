using CommandQuery;
using Microsoft.Extensions.Caching.Distributed;
using SCMM.Discord.Data.Models;
using SCMM.Shared.Client.Extensions;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Server.Queries
{
    public class ListLatestSystemUpdateMessagesRequest : IQuery<ListLatestSystemUpdateMessagesResponse>
    {
    }

    public class ListLatestSystemUpdateMessagesResponse
    {
        public IEnumerable<SystemUpdateMessageDTO> Messages { get; set; }
    }

    public class ListLatestSystemUpdateMessages : IQueryHandler<ListLatestSystemUpdateMessagesRequest, ListLatestSystemUpdateMessagesResponse>
    {
        private readonly IDistributedCache _cache;

        public ListLatestSystemUpdateMessages(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<ListLatestSystemUpdateMessagesResponse> HandleAsync(ListLatestSystemUpdateMessagesRequest request)
        {
            var latestSystemUpdates = await _cache.GetJsonAsync<TextMessage[]>(Constants.LatestSystemUpdatesCacheKey);
            if (latestSystemUpdates == null)
            {
                return new ListLatestSystemUpdateMessagesResponse();
            }

            return new ListLatestSystemUpdateMessagesResponse()
            {
                Messages = latestSystemUpdates
                    .OrderByDescending(x => x.Timestamp)
                    .Select(x => new SystemUpdateMessageDTO()
                    {
                        Timestamp = x.Timestamp,
                        Description = x.Content,
                        Media = x.Attachments.ToDictionary(k => k.Url, v => v.ContentType)
                    })
                    .ToArray()
            };
        }
    }
}
