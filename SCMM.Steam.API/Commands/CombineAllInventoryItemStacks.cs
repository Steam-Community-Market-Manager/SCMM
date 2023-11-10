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
    public class CombineAllInventoryItemStacksRequest : ICommand
    {
        public string ProfileId { get; set; }

        public string ApiKey { get; set; }

        public bool StackUntradableAndUnmarketable { get; set; } = false;
    }

    public class CombineAllInventoryItemStacks : ICommandHandler<CombineAllInventoryItemStacksRequest>
    {
        private readonly SteamDbContext _db;
        private readonly SteamWebApiClient _steamWebApiClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public CombineAllInventoryItemStacks(SteamDbContext db, SteamWebApiClient steamWebApiClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _steamWebApiClient = steamWebApiClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task HandleAsync(CombineAllInventoryItemStacksRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            var items = await _db.SteamProfileInventoryItems
                .Include(x => x.App)
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => request.StackUntradableAndUnmarketable || x.TradableAndMarketable)
                .ToListAsync();

            var itemGroups = items
                .GroupBy(x => x.DescriptionId)
                .Where(x => x.Count() > 1);

            foreach (var itemGroup in itemGroups)
            {
                var destinationItem = itemGroup.MinBy(x => x.SteamId);
                var destinationItemId = UInt64.Parse(destinationItem.SteamId);
                var destinationAppId = UInt64.Parse(destinationItem.App.SteamId);
                var sourceItems = itemGroup.Except(new[] { destinationItem }).Select(x => new
                {
                    Item = x,
                    ItemId = UInt64.Parse(x.SteamId),
                    Quantity = x.Quantity
                });

                foreach (var sourceItem in sourceItems)
                {
                    var response = await _steamWebApiClient.InventoryServiceCombineItemStackAsync(new CombineItemStacksJsonRequest()
                    {
                        Key = request.ApiKey,
                        AppId = destinationAppId,
                        SteamId = resolvedId.SteamId64.Value,
                        FromItemId = sourceItem.ItemId,
                        DestItemId = destinationItemId,
                        Quantity = (uint)sourceItem.Quantity
                    });

                    if (response.Any())
                    {
                        foreach (var item in response)
                        {
                            var inventoryItem = items.FirstOrDefault(x => x.SteamId == item.ItemId);
                            if (inventoryItem != null)
                            {
                                inventoryItem.Quantity = (int)item.Quantity;
                                if (inventoryItem.Quantity <= 0)
                                {
                                    _db.SteamProfileInventoryItems.Remove(inventoryItem);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new SteamRequestException("Steam reported failure, some items have have been modified", HttpStatusCode.BadRequest);
                    }
                }

            }
        }
    }
}
