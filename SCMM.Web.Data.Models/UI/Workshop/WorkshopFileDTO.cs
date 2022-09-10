using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Workshop
{
    public class WorkshopFileDTO : ICanBeInteractedWith
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public ulong? CreatorId { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public string ItemType { get; set; }

        public string ItemShortName { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string PreviewUrl { get; set; }

        public long? SubscriptionsCurrent { get; set; }

        public long? SubscriptionsLifetime { get; set; }

        public long? FavouritedCurrent { get; set; }

        public long? FavouritedLifetime { get; set; }

        public long? Views { get; set; }

        public uint? VotesUp { get; set; }

        public uint? VotesDown { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        public ItemInteractionDTO[] Actions { get; set; }
    }
}
