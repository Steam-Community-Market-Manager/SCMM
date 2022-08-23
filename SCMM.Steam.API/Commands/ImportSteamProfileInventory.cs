using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamProfileInventoryRequest : ICommand<ImportSteamProfileInventoryResponse>
    {
        public string ProfileId { get; set; }

        public string AppId { get; set; }

        /// <summary>
        /// If true, inventory will always be fetched. If false, inventory is cached for 1 hour.
        /// </summary>
        public bool Force { get; set; } = false;
    }

    public class ImportSteamProfileInventoryResponse
    {
        public SteamProfile Profile { get; set; }
    }

    public class ImportSteamProfileInventory : ICommandHandler<ImportSteamProfileInventoryRequest, ImportSteamProfileInventoryResponse>
    {
        private readonly SteamDbContext _db;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly SteamService _steamService;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamProfileInventory(SteamDbContext db, SteamCommunityWebClient communityClient, SteamService steamService, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _communityClient = communityClient;
            _steamService = steamService;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamProfileInventoryResponse> HandleAsync(ImportSteamProfileInventoryRequest request, CancellationToken cancellationToken)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            // If the profile id does not yet exist, fetch it now
            if (!resolvedId.Exists)
            {
                _ = await _commandProcessor.ProcessAsync(new ImportSteamProfileRequest()
                {
                    ProfileId = request.ProfileId
                });
            }

            // Load the profile
            var profileInventory = await _db.SteamProfiles
                .Include(x => x.InventoryItems).ThenInclude(x => x.App)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Description)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Currency)
                .Where(x => x.Id == resolvedId.ProfileId)
                .Select(x => new
                {
                    Profile = x,
                    TotalItems = x.InventoryItems.Count,
                    LastUpdatedOn = x.LastUpdatedInventoryOn
                })
                .FirstOrDefaultAsync();

            // If the profile inventory is less than 1 hour old and we aren't forcing an update, just return the current inventory
            var profile = profileInventory?.Profile ?? resolvedId.Profile;
            if (profile != null && profileInventory != null && profileInventory.TotalItems > 0 && DateTime.Now.Subtract(TimeSpan.FromHours(1)) < profileInventory.LastUpdatedOn && request.Force == false)
            {
                return new ImportSteamProfileInventoryResponse()
                {
                    Profile = profile
                };
            }

            // Load the apps
            var apps = await _db.SteamApps
                .Where(x => String.IsNullOrEmpty(request.AppId) || x.SteamId == request.AppId)
                .ToListAsync();
            if (!apps.Any())
            {
                return null;
            }

            // Fetch the profiles inventory for each of the apps we monitor
            foreach (var app in apps)
            {
                var steamInventoryItems = await FetchInventoryRecursive(profile, app);
                if (steamInventoryItems == null || profile.Privacy == SteamVisibilityType.Private)
                {
                    break;
                }

                // Add assets
                var missingAssets = steamInventoryItems
                    .Where(x => !profile.InventoryItems.Any(y => y.AppId == app.Id && y.SteamId == x.Key.AssetId.ToString()))
                    .ToList();
                var knownAssets = await _db.SteamAssetDescriptions
                    .Where(x => x.AppId == app.Id)
                    .Select(x => new
                    {
                        Id = x.Id,
                        ClassId = x.ClassId,
                        IsDrop = (x.IsSpecialDrop || x.IsTwitchDrop)
                    })
                    .ToListAsync();

                foreach (var asset in missingAssets)
                {
                    var assetDescription = knownAssets.FirstOrDefault(x => x.ClassId == asset.Key.ClassId);
                    if (assetDescription == null)
                    {
                        // NOTE: Only import new assets from apps we know are ready
                        // TODO: Remove this check one day
                        if (app.IsActive)
                        {
                            var importAssetDescription = await _commandProcessor.ProcessAsync(new ImportSteamAssetDescriptionRequest()
                            {
                                AppId = ulong.Parse(app.SteamId),
                                AssetClassId = asset.Key.ClassId
                                // TODO: Test this more. It seems there is missing data sometimes so we'll fetch the full details from Steam instead
                                //AssetClass = inventory.Descriptions.FirstOrDefault(x => x.ClassId == asset.ClassId)
                            });
                            assetDescription = new
                            {
                                Id = importAssetDescription.AssetDescription.Id,
                                ClassId = importAssetDescription.AssetDescription.ClassId,
                                IsDrop = (importAssetDescription.AssetDescription.IsSpecialDrop || importAssetDescription.AssetDescription.IsTwitchDrop)
                            };
                        }
                    }
                    if (assetDescription == null)
                    {
                        continue;
                    }
                    var inventoryItem = new SteamProfileInventoryItem()
                    {
                        SteamId = asset.Key.AssetId.ToString(),
                        Profile = profile,
                        ProfileId = profile.Id,
                        App = app,
                        AppId = app.Id,
                        DescriptionId = assetDescription.Id,
                        Quantity = (int)asset.Key.Amount,
                        TradableAndMarketable = (asset.Key.InstanceId == Constants.SteamAssetDefaultInstanceId)
                        // TODO: TradableAndMarketablAfter = asset.Value.OwnerDescriptions.FirstOrDefault(x => x.Value == Constants.SteamInventoryItemMarketableAndTradableAfterOwnerDescriptionRegex)
                    };

                    // If this item is a special/twitch drop, automatically mark it as a drop
                    if (assetDescription.IsDrop)
                    {
                        inventoryItem.AcquiredBy = SteamProfileInventoryItemAcquisitionType.Drop;
                    }

                    profile.InventoryItems.Add(inventoryItem);
                }

                // Update assets
                foreach (var asset in steamInventoryItems)
                {
                    var existingAsset = profile.InventoryItems.FirstOrDefault(x => x.AppId == app.Id && x.SteamId == asset.Key.AssetId.ToString());
                    if (existingAsset != null)
                    {
                        existingAsset.Quantity = (int)asset.Key.Amount;
                        existingAsset.TradableAndMarketable = (asset.Key.InstanceId == Constants.SteamAssetDefaultInstanceId);
                        // TODO: existingAsset.TradableAndMarketablAfter = asset.Value.OwnerDescriptions.FirstOrDefault(x => x.Value == Constants.SteamInventoryItemMarketableAndTradableAfterOwnerDescriptionRegex);
                    }
                }

                // Remove assets
                var removedAssets = profile.InventoryItems
                    .Where(x => x.AppId == app.Id)
                    .Where(x => !steamInventoryItems.Any(y => y.Key.AssetId.ToString() == x.SteamId))
                    .ToList();
                foreach (var asset in removedAssets)
                {
                    profile.InventoryItems.Remove(asset);
                }

                // Update last inventory update timestamp
                profile.LastUpdatedInventoryOn = DateTimeOffset.Now;
            }

            return new ImportSteamProfileInventoryResponse()
            {
                Profile = profile
            };
        }

        private async Task<IDictionary<SteamInventoryAsset, SteamAssetClass>> FetchInventoryRecursive(SteamProfile profile, SteamApp app, ulong? startAssetId = null, int count = SteamInventoryPaginatedJsonRequest.MaxPageSize)
        {
            var inventory = (SteamInventoryPaginatedJsonResponse)null;
            var inventoryItems = new Dictionary<SteamInventoryAsset, SteamAssetClass>();

            try
            {
                // Fetch assets
                inventory = await _communityClient.GetInventoryPaginated(new SteamInventoryPaginatedJsonRequest()
                {
                    AppId = app.SteamId,
                    SteamId = profile.SteamId,
                    StartAssetId = startAssetId,
                    Count = count,
                    NoRender = true
                });
                if (inventory == null)
                {
                    // Inventory is probably private
                    profile.Privacy = SteamVisibilityType.Private;
                    return inventoryItems;
                }
                if (inventory.Assets?.Any() == true)
                {
                    inventoryItems.AddRange(
                        inventory.Assets.ToDictionary(
                            k => k,
                            v => inventory.Descriptions?.FirstOrDefault(x => x.ClassId == v.ClassId && x.InstanceId == v.InstanceId)
                        )
                    );
                    profile.Privacy = SteamVisibilityType.Public;
                }
            }
            catch (SteamRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    profile.Privacy = SteamVisibilityType.Private;
                    return null;
                }
                else
                {
                    throw;
                }
            }

            // If there are more assets to fetch, make a call to the next page
            if (inventory?.Assets?.Count >= SteamInventoryPaginatedJsonRequest.MaxPageSize)
            {
                var moreInventoryItems = await FetchInventoryRecursive(profile, app, inventory.Assets.LastOrDefault()?.AssetId);
                if (moreInventoryItems?.Any() == true)
                {
                    inventoryItems.AddRange(moreInventoryItems);
                }
            }

            return inventoryItems;
        }
    }
}
