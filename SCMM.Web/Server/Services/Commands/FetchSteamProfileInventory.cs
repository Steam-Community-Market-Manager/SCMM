using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands
{
    public class FetchSteamProfileInventoryRequest : ICommand
    {
        public Guid Id { get; set; }

        /// <summary>
        /// If true, inventory will always be fetched. If false, inventory is cached for 3 hours.
        /// </summary>
        public bool Force { get; set; } = false;
    }

    public class FetchSteamProfileInventory : ICommandHandler<FetchSteamProfileInventoryRequest>
    {
        private readonly ScmmDbContext _db;
        private readonly SteamCommunityClient _communityClient;
        private readonly SteamService _steamService;

        public FetchSteamProfileInventory(ScmmDbContext db, SteamCommunityClient communityClient, SteamService steamService)
        {
            _db = db;
            _communityClient = communityClient;
            _steamService = steamService;
        }

        public async Task HandleAsync(FetchSteamProfileInventoryRequest request)
        {
            // Load the profile
            var profileInventory = _db.SteamProfiles
                .Include(x => x.InventoryItems).ThenInclude(x => x.App)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Description)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Currency)
                .Where(x => x.Id == request.Id)
                .Select(x => new
                {
                    Profile = x,
                    TotalItems = x.InventoryItems.Count,
                    LastUpdatedOn = x.LastUpdatedInventoryOn
                })
                .FirstOrDefault();

            // If the profile inventory is less than 3 hours old and we aren't forcing an update, just return the current inventory
            var profile = profileInventory?.Profile;
            if (profile != null && profileInventory.TotalItems > 0 && DateTime.Now.Subtract(TimeSpan.FromHours(3)) < profileInventory.LastUpdatedOn && request.Force == false)
            {
                return;
            }

            // If the profile is null, double check that it isnt transient (newly created)
            if (profile == null)
            {
                profile = _db.SteamProfiles.Local.FirstOrDefault(x => x.Id == request.Id);
                if (profile == null)
                {
                    return;
                }
            }

            // Load the language
            var language = _db.SteamLanguages.AsNoTracking().FirstOrDefault(x => x.IsDefault);
            if (language == null)
            {
                return;
            }

            // Load the apps
            var apps = _db.SteamApps.ToList();
            if (!apps.Any())
            {
                return;
            }

            // Fetch the profiles inventory for each of the apps we monitor
            foreach (var app in apps)
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
                    profile.Privacy = Shared.Data.Models.Steam.SteamVisibilityType.Private;
                    continue;
                }
                if (inventory.Assets?.Any() != true)
                {
                    // Inventory doesn't have any items for this app
                    continue;
                }

                // Add assets
                var missingAssets = inventory.Assets
                    .Where(x => !profile.InventoryItems.Any(y => y.SteamId == x.AssetId))
                    .ToList();
                foreach (var asset in missingAssets)
                {
                    var assetDescription = await _steamService.AddOrUpdateAssetDescription(app, language.SteamId, UInt64.Parse(asset.ClassId));
                    if (assetDescription == null)
                    {
                        continue;
                    }
                    var inventoryItem = new SteamProfileInventoryItem()
                    {
                        SteamId = asset.AssetId,
                        Profile = profile,
                        ProfileId = profile.Id,
                        App = app,
                        AppId = app.Id,
                        Description = assetDescription,
                        DescriptionId = assetDescription.Id,
                        Quantity = asset.Amount
                    };

                    profile.InventoryItems.Add(inventoryItem);
                }

                // Update assets
                foreach (var asset in inventory.Assets)
                {
                    var existingAsset = profile.InventoryItems.FirstOrDefault(x => x.SteamId == asset.AssetId);
                    if (existingAsset != null)
                    {
                        existingAsset.Quantity = asset.Amount;
                    }
                }

                // Remove assets
                var removedAssets = profile.InventoryItems
                    .Where(x => !inventory.Assets.Any(y => y.AssetId == x.SteamId))
                    .ToList();
                foreach (var asset in removedAssets)
                {
                    profile.InventoryItems.Remove(asset);
                }

                // Update last inventory update timestamp and privacy state
                profile.LastUpdatedInventoryOn = DateTimeOffset.Now;
                profile.Privacy = Shared.Data.Models.Steam.SteamVisibilityType.Public;
            }
        }
    }
}
