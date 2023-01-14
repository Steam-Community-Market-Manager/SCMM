using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Store.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamWorkshopFile : SteamItem
    {
        public SteamWorkshopFile()
        {
            Tags = new PersistableStringDictionary();
            Previews = new PersistableMediaDictionary();
        }

        public ulong? CreatorId { get; set; }

        public Guid? CreatorProfileId { get; set; }

        public SteamProfile CreatorProfile { get; set; }

        /// <summary>
        /// e.g. Large Wood Box, Sheet Metal Door, etc
        /// </summary>
        public string ItemType { get; set; }

        /// <summary>
        /// e.g. door.hinged.metal, wall.frame.garagedoor, etc
        /// </summary>
        public string ItemShortName { get; set; }

        /// <summary>
        /// e.g. Blackout, Whiteout, etc
        /// </summary>
        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public new string Description { get; set; }

        [Required]
        public PersistableStringDictionary Tags { get; set; }

        public string PreviewUrl { get; set; }

        [Required]
        public PersistableMediaDictionary Previews { get; set; }

        public long? SubscriptionsCurrent { get; set; }

        public long? SubscriptionsLifetime { get; set; }

        public long? FavouritedCurrent { get; set; }

        public long? FavouritedLifetime { get; set; }

        public long? Views { get; set; }

        public uint? VotesUp { get; set; }

        public uint? VotesDown { get; set; }

        public bool IsAccepted { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        /// <summary>
        /// Last time this workshop file was updated from Steam
        /// </summary>
        public DateTimeOffset? TimeRefreshed { get; set; }

        public IEnumerable<ItemInteraction> GetInteractions()
        {
            if (!String.IsNullOrEmpty(SteamId))
            {
                yield return new ItemInteraction
                {
                    Icon = "fa-tools",
                    Name = "View Workshop",
                    Url = new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = SteamId
                    }
                };
            }
        }
    }
}
