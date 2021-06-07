using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamProfileInventoryRequest : ICommand<ImportSteamProfileInventoryResponse>
    {
        public string ProfileId { get; set; }

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

        public async Task<ImportSteamProfileInventoryResponse> HandleAsync(ImportSteamProfileInventoryRequest request)
        {
            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            // If the profile id does not yet exist, fetch it now
            if (!resolvedId.Exists)
            {
                _ = await _commandProcessor.ProcessWithResultAsync(new ImportSteamProfileRequest()
                {
                    ProfileId = request.ProfileId
                });
            }

            // Load the profile
            var profileInventory = await _db.SteamProfiles
                .Include(x => x.InventoryItems).ThenInclude(x => x.App)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Description)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Currency)
                .Where(x => x.Id == resolvedId.Id)
                .Select(x => new
                {
                    Profile = x,
                    TotalItems = x.InventoryItems.Count,
                    LastUpdatedOn = x.LastUpdatedInventoryOn
                })
                .FirstOrDefaultAsync();

            // If the profile inventory is less than 1 hour old and we aren't forcing an update, just return the current inventory
            var profile = profileInventory?.Profile;
            if (profile != null && profileInventory.TotalItems > 0 && DateTime.Now.Subtract(TimeSpan.FromHours(1)) < profileInventory.LastUpdatedOn && request.Force == false)
            {
                return new ImportSteamProfileInventoryResponse()
                {
                    Profile = profile
                };
            }

            // If the profile is null, double check that it isnt transient (newly created)
            if (profile == null)
            {
                profile = _db.SteamProfiles.Local.FirstOrDefault(x => x.SteamId == resolvedId.SteamId || x.ProfileId == resolvedId.ProfileId);
                if (profile == null)
                {
                    return null;
                }
            }

            // Load the language
            var language = await _db.SteamLanguages.AsNoTracking().FirstOrDefaultAsync(x => x.IsDefault);
            if (language == null)
            {
                return null;
            }

            // Load the apps
            var apps = await _db.SteamApps.ToListAsync();
            if (!apps.Any())
            {
                return null;
            }

            // Fetch the profiles inventory for each of the apps we monitor
            foreach (var app in apps)
            {
                try
                {
                    // Fetch assets
                    var inventory = await _communityClient.GetInventoryPaginated(new SteamInventoryPaginatedJsonRequest()
                    {
                        AppId = app.SteamId,
                        SteamId = profile.SteamId,
                        Start = 1,
                        Count = SteamInventoryPaginatedJsonRequest.MaxPageSize,
                        NoRender = true
                    });
                    if (inventory == null)
                    {
                        // Inventory is probably private
                        profile.Privacy = SCMM.Steam.Data.Models.Enums.SteamVisibilityType.Private;
                        continue;
                    }
                    if (inventory.Assets?.Any() != true)
                    {
                        // Inventory doesn't have any items for this app
                        continue;
                    }

                    // Add assets
                    var missingAssets = inventory.Assets
                        .Where(x => !profile.InventoryItems.Any(y => y.SteamId == x.AssetId.ToString()))
                        .ToList();
                    foreach (var asset in missingAssets)
                    {
                        var assetDescription = await _steamService.AddOrUpdateAssetDescription(app, language.SteamId, asset.ClassId);
                        if (assetDescription == null)
                        {
                            continue;
                        }
                        var inventoryItem = new SteamProfileInventoryItem()
                        {
                            SteamId = asset.AssetId.ToString(),
                            Profile = profile,
                            ProfileId = profile.Id,
                            App = app,
                            AppId = app.Id,
                            Description = assetDescription,
                            DescriptionId = assetDescription.Id,
                            Quantity = (int)asset.Amount
                        };

                        profile.InventoryItems.Add(inventoryItem);
                    }

                    // Update assets
                    foreach (var asset in inventory.Assets)
                    {
                        var existingAsset = profile.InventoryItems.FirstOrDefault(x => x.SteamId == asset.AssetId.ToString());
                        if (existingAsset != null)
                        {
                            existingAsset.Quantity = (int)asset.Amount;
                        }
                    }

                    // Remove assets
                    var removedAssets = profile.InventoryItems
                        .Where(x => !inventory.Assets.Any(y => y.AssetId.ToString() == x.SteamId))
                        .ToList();
                    foreach (var asset in removedAssets)
                    {
                        profile.InventoryItems.Remove(asset);
                    }

                    // Update last inventory update timestamp and privacy state
                    profile.LastUpdatedInventoryOn = DateTimeOffset.Now;
                    profile.Privacy = SCMM.Steam.Data.Models.Enums.SteamVisibilityType.Public;
                }
                catch (SteamRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        profile.Privacy = SCMM.Steam.Data.Models.Enums.SteamVisibilityType.Private;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return new ImportSteamProfileInventoryResponse()
            {
                Profile = profile
            };
        }
    }
}
