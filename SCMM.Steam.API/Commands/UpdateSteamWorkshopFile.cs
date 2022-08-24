using CommandQuery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using Steam.Models;
using SteamWebAPI2.Utilities;

namespace SCMM.Steam.API.Commands
{
    public class UpdateSteamWorkshopFileRequest : ICommand<UpdateSteamWorkshopFileResponse>
    {
        public SteamWorkshopFile WorkshopFile { get; set; }

        public PublishedFileDetailsModel PublishedFile { get; set; }

        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class UpdateSteamWorkshopFileResponse
    {
        public SteamWorkshopFile WorkshopFile { get; set; }
    }

    public class UpdateSteamWorkshopFile : ICommandHandler<UpdateSteamWorkshopFileRequest, UpdateSteamWorkshopFileResponse>
    {
        private readonly ILogger<UpdateSteamWorkshopFile> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public UpdateSteamWorkshopFile(ILogger<UpdateSteamWorkshopFile> logger, SteamDbContext db, IConfiguration cfg, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<UpdateSteamWorkshopFileResponse> HandleAsync(UpdateSteamWorkshopFileRequest request, CancellationToken cancellationToken)
        {
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
            var workshopFile = request.WorkshopFile;
            if (workshopFile == null)
            {
                throw new ArgumentNullException(nameof(request.WorkshopFile));
            }

            // Parse workshop file details
            if (request.PublishedFile != null)
            {
                var publishedFile = request.PublishedFile;
                workshopFile.SteamId = publishedFile.PublishedFileId.ToString();
                workshopFile.CreatorId = (publishedFile.Creator > 0 ? publishedFile.Creator : null);
                workshopFile.Name = publishedFile.Title;
                workshopFile.Description = publishedFile.Description;
                workshopFile.PreviewUrl = publishedFile.PreviewUrl?.ToString();
                workshopFile.SubscriptionsCurrent = (long?)publishedFile.Subscriptions;
                workshopFile.SubscriptionsLifetime = (long?)publishedFile.LifetimeSubscriptions;
                workshopFile.FavouritedCurrent = (long?)publishedFile.Favorited;
                workshopFile.FavouritedLifetime = (long?)publishedFile.LifetimeFavorited;
                workshopFile.Views = (long?)publishedFile.Views;
                workshopFile.IsAccepted = (workshopFile.IsAccepted | (publishedFile.LifetimeSubscriptions > 0));
                workshopFile.TimeCreated = workshopFile.TimeCreated.Earliest(publishedFile.TimeCreated > DateTime.MinValue ? publishedFile.TimeCreated : null);
                workshopFile.TimeUpdated = workshopFile.TimeUpdated.Latest(publishedFile.TimeUpdated > DateTime.MinValue ? publishedFile.TimeUpdated : null);

                // Parse creator
                if (workshopFile.CreatorProfileId == null && publishedFile.Creator > 0)
                {
                    try
                    {
                        var importedProfile = await _commandProcessor.ProcessAsync(
                            new ImportSteamProfileRequest()
                            {
                                ProfileId = publishedFile.Creator.ToString()
                            }
                        );
                        if (importedProfile?.Profile != null)
                        {
                            workshopFile.CreatorProfile = importedProfile.Profile;
                            workshopFile.CreatorProfileId = importedProfile.Profile.Id;
                        }
                    }
                    catch (Exception)
                    {
                        // Account has probably been deleted, not a big deal, continue on...
                    }
                }

                // Parse tags
                if (publishedFile.Tags != null)
                {
                    workshopFile.Tags = new PersistableStringDictionary(publishedFile.Tags.ToDictionary(k => k, v => v));
                }

                // Parse item type
                workshopFile.ItemType = workshopFile.Tags.FirstOrDefault(t => !Constants.SteamIgnoredWorkshopTags.Contains(t.Value)).Value.WorkshopTagToRustItemType();
                workshopFile.ItemShortName = workshopFile.ItemType.ToRustItemShortName();
            }

            // Parse asset description details
            if (request.AssetDescription != null)
            {
                var assetDescription = request.AssetDescription;
                workshopFile.DescriptionId = assetDescription.Id;
            }

            // Cleanup the description text
            workshopFile.Description = workshopFile.Description
                ?.Trim("&nbsp;")
                ?.Trim(' ', '.', '\t', '\n', '\r');

            // Update last checked on (unless this is a newly created item)
            if (!workshopFile.IsTransient)
            {
                workshopFile.TimeRefreshed = DateTimeOffset.Now;
            }

            return new UpdateSteamWorkshopFileResponse
            {
                WorkshopFile = workshopFile
            };
        }
    }
}
