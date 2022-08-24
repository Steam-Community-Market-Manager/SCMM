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
    public class SplitInventoryItemStackRequest : ICommand<SplitInventoryItemStackResponse>
    {
        public string ProfileId { get; set; }

        public string ApiKey { get; set; }

        public ulong ItemId { get; set; }

        public uint Quantity { get; set; }

        public bool StackNewItems { get; set; } = true;
    }

    public class SplitInventoryItemStackResponse
    {
        public IEnumerable<SteamProfileInventoryItem> Items { get; set; }
    }

    public class SplitInventoryItemStack : ICommandHandler<SplitInventoryItemStackRequest, SplitInventoryItemStackResponse>
    {
        private readonly SteamDbContext _db;
        private readonly SteamWebApiClient _steamWebApiClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public SplitInventoryItemStack(SteamDbContext db, SteamWebApiClient steamWebApiClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _steamWebApiClient = steamWebApiClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<SplitInventoryItemStackResponse> HandleAsync(SplitInventoryItemStackRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            var sourceItem = await _db.SteamProfileInventoryItems
                .Include(x => x.App)
                .Where(x => x.ProfileId == resolvedId.ProfileId)
                .Where(x => x.SteamId == request.ItemId.ToString())
                .FirstOrDefaultAsync();

            var items = new List<SteamProfileInventoryItem>()
            {
                sourceItem
            };

            var appId = UInt64.Parse(sourceItem.App.SteamId);
            var quantityRemaining = request.Quantity;
            while(quantityRemaining > 0)
            {
                var quantityToSplit = (request.StackNewItems ? quantityRemaining : 1);
                var response = await _steamWebApiClient.InventoryServiceSplitItemStack(new SplitItemStackJsonRequest()
                {
                    Key = request.ApiKey,
                    AppId = appId,
                    SteamId = resolvedId.SteamId64.Value,
                    ItemId = request.ItemId,
                    Quantity = quantityToSplit
                });
                if (response?.Any() == true)
                {
                    foreach (var item in response)
                    {
                        var inventoryItem = items.FirstOrDefault(x => x.SteamId == item.ItemId);
                        if (inventoryItem != null)
                        {
                            inventoryItem.Quantity = (int)item.Quantity;
                        }
                        else
                        {
                            items.Add(inventoryItem = new SteamProfileInventoryItem()
                            {
                                SteamId = item.ItemId,
                                Profile = sourceItem.Profile,
                                ProfileId = sourceItem.ProfileId,
                                App = sourceItem.App,
                                AppId = sourceItem.AppId,
                                Description = sourceItem.Description,
                                DescriptionId = sourceItem.DescriptionId,
                                Currency = sourceItem.Currency,
                                CurrencyId = sourceItem.CurrencyId,
                                AcquiredBy = sourceItem.AcquiredBy,
                                BuyPrice = sourceItem.BuyPrice,
                                Quantity = (int)item.Quantity,
                                TradableAndMarketable = sourceItem.TradableAndMarketable,
                                TradableAndMarketablAfter = sourceItem.TradableAndMarketablAfter,
                                Flags = sourceItem.Flags,
                            });

                            _db.SteamProfileInventoryItems.Add(inventoryItem);
                        }
                    }
                }
                else
                {
                    throw new SteamRequestException($"Steam reported failure, {(request.StackNewItems ? "no" : "some")} items were modified", HttpStatusCode.BadRequest);
                }

                quantityRemaining -= quantityToSplit;
            }

            return new SplitInventoryItemStackResponse()
            {
                Items = items
            };
        }
    }
}
