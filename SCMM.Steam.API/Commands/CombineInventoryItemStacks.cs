using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService;
using SCMM.Steam.Data.Store;
using System.Net;

namespace SCMM.Steam.API.Commands
{
    public class CombineInventoryItemStacksRequest : ICommand<CombineInventoryItemStacksResponse>
    {
        public string ProfileId { get; set; }

        public string ApiKey { get; set; }

        public IDictionary<ulong, uint> SourceItems { get; set; }

        public ulong DestinationItemId { get; set; }

    }

    public class CombineInventoryItemStacksResponse
    {
        public SteamProfileInventoryItem Item { get; set; }
    }

    public class CombineInventoryItemStacks : ICommandHandler<CombineInventoryItemStacksRequest, CombineInventoryItemStacksResponse>
    {
        private readonly SteamDbContext _db;
        private readonly SteamWebApiClient _steamWebApiClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public CombineInventoryItemStacks(SteamDbContext db, SteamWebApiClient steamWebApiClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _steamWebApiClient = steamWebApiClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<CombineInventoryItemStacksResponse> HandleAsync(CombineInventoryItemStacksRequest request, CancellationToken cancellationToken)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            var itemIds = request.SourceItems.Keys.Select(x => x.ToString()).Union(new [] { request.DestinationItemId.ToString() });
            var items = await _db.SteamProfileInventoryItems
                .Include(x => x.App)
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => itemIds.Contains(x.SteamId))
                .ToListAsync();

            var destinationItem = items.FirstOrDefault(x => x.SteamId == request.DestinationItemId.ToString());
            var destinationItemId = request.DestinationItemId;
            var destinationAppId = UInt64.Parse(destinationItem.App.SteamId);
            var sourceItems = request.SourceItems.Join(items, x => x.Key.ToString(), x => x.SteamId, (x, y) => new
            {
                Item = y,
                ItemId = x.Key,
                Quantity = x.Value
            });

            foreach (var sourceItem in sourceItems)
            {
                var response = await _steamWebApiClient.InventoryServiceCombineItemStack(new CombineItemStacksJsonRequest()
                {
                    Key = request.ApiKey,
                    AppId = destinationAppId,
                    SteamId = resolvedId.SteamId64.Value,
                    FromItemId = sourceItem.ItemId,
                    DestItemId = destinationItemId,
                    Quantity = (uint) sourceItem.Quantity
                });

                if (response.Any())
                {
                    foreach (var item in response)
                    {
                        var inventoryItem = items.FirstOrDefault(x => x.SteamId == item.ItemId);
                        if (inventoryItem != null)
                        {
                            inventoryItem.Quantity = (int) item.Quantity;
                            if (inventoryItem.Quantity <= 0)
                            {
                                _db.SteamProfileInventoryItems.Remove(inventoryItem);
                            }
                        }
                    }
                }
                else
                {
                    throw new SteamRequestException("Steam reported failure, no items were modified", HttpStatusCode.BadRequest);
                }
            }

            return new CombineInventoryItemStacksResponse()
            {
                Item = destinationItem
            };
        }
    }
}
