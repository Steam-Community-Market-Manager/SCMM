using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFileDetails
    {
        [JsonPropertyName("result")]
        public int Result { get; set; }

        [JsonPropertyName("publishedfileid")]
        public ulong PublishedFileId { get; set; }

        [JsonPropertyName("creator")]
        public ulong Creator { get; set; }

        [JsonPropertyName("creator_appid")]
        public ulong CreatorAppId { get; set; }

        [JsonPropertyName("consumer_appid")]
        public ulong ConsumerAppId { get; set; }

        [JsonPropertyName("consumer_shortcutid")]
        public ulong ConsumerShortcutId { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("file_size")]
        public ulong FileSize { get; set; }

        [JsonPropertyName("preview_file_size")]
        public ulong PreviewFileSize { get; set; }

        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("hcontent_file")]
        public string HandleContentFile { get; set; }

        [JsonPropertyName("hcontent_preview")]
        public string HandleContentPreview { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("file_description")]
        public string FileDescription { get; set; }

        [JsonPropertyName("short_description")]
        public string ShortDescription { get; set; }

        [JsonPropertyName("time_created")]
        public ulong TimeCreated { get; set; }

        [JsonPropertyName("time_updated")]
        public ulong TimeUpdated { get; set; }

        [JsonPropertyName("visibility")]
        public uint Visibility { get; set; }

        [JsonPropertyName("flags")]
        public ulong Flags { get; set; }

        [JsonPropertyName("workshop_file")]
        public bool WorkshopFile { get; set; }

        [JsonPropertyName("workshop_accepted")]
        public bool WorkshopAccepted { get; set; }

        [JsonPropertyName("show_subscribe_all")]
        public bool ShowSubscribeAll { get; set; }

        [JsonPropertyName("num_comments_public")]
        public ulong NumCommentsPublic { get; set; }

        [JsonPropertyName("banned")]
        public bool Banned { get; set; }

        [JsonPropertyName("ban_reason")]
        public string BanReason { get; set; }

        [JsonPropertyName("banner")]
        public ulong Banner { get; set; }

        [JsonPropertyName("can_be_deleted")]
        public bool CanBeDeleted { get; set; }

        [JsonPropertyName("app_name")]
        public string AppName { get; set; }

        [JsonPropertyName("file_type")]
        public uint FileType { get; set; }

        [JsonPropertyName("can_subscribe")]
        public bool CanSubscribe { get; set; }

        [JsonPropertyName("subscriptions")]
        public ulong Subscriptions { get; set; }

        [JsonPropertyName("favorited")]
        public ulong Favorited { get; set; }

        [JsonPropertyName("followers")]
        public ulong Followers { get; set; }

        [JsonPropertyName("lifetime_subscriptions")]
        public ulong LifetimeSubscriptions { get; set; }

        [JsonPropertyName("lifetime_favorited")]
        public ulong LifetimeFavorited { get; set; }

        [JsonPropertyName("lifetime_followers")]
        public ulong LifetimeFollowers { get; set; }

        [JsonPropertyName("lifetime_playtime")]
        public ulong LifetimePlaytime { get; set; }

        [JsonPropertyName("lifetime_playtime_sessions")]
        public ulong LifetimePlaytimeSessions { get; set; }

        [JsonPropertyName("views")]
        public ulong Views { get; set; }

        [JsonPropertyName("num_children")]
        public ulong NumChildren { get; set; }

        [JsonPropertyName("num_reports")]
        public ulong NumReports { get; set; }

        [JsonPropertyName("previews")]
        public List<PublishedFilePreview> Previews { get; set; }

        [JsonPropertyName("tags")]
        public List<PublishedFileTag> Tags { get; set; }

        [JsonPropertyName("vote_data")]
        public PublishedFileVoteData VoteData { get; set; }

        [JsonPropertyName("playtime_stats")]
        public PublishedFilePlaytimeStats PlaytimeStats { get; set; }

        [JsonPropertyName("language")]
        public uint Language { get; set; }

        [JsonPropertyName("maybe_inappropriate_sex")]
        public bool MaybeInappropriateSex { get; set; }

        [JsonPropertyName("maybe_inappropriate_violence")]
        public bool MaybeInappropriateViolence { get; set; }

        [JsonPropertyName("revision_change_number")]
        public uint RevisionChangeNumber { get; set; }

        [JsonPropertyName("revision")]
        public uint Revision { get; set; }

        [JsonPropertyName("ban_text_check_result")]
        public bool Ban_TextCheckResult { get; set; }
    }
}
