using Newtonsoft.Json;

namespace SCMM.Steam.Data.Models.WebApi.Models
{
    public class PublishedFileDetails
    {
        [JsonProperty("result")]
        public int Result { get; set; }

        [JsonProperty("publishedfileid")]
        public ulong PublishedFileId { get; set; }

        [JsonProperty("creator")]
        public ulong Creator { get; set; }

        [JsonProperty("creator_appid")]
        public ulong CreatorAppId { get; set; }

        [JsonProperty("consumer_appid")]
        public ulong ConsumerAppId { get; set; }

        [JsonProperty("consumer_shortcutid")]
        public ulong ConsumerShortcutId { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("file_size")]
        public ulong FileSize { get; set; }

        [JsonProperty("preview_file_size")]
        public ulong PreviewFileSize { get; set; }

        [JsonProperty("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("hcontent_file")]
        public string HandleContentFile { get; set; }

        [JsonProperty("hcontent_preview")]
        public string HandleContentPreview { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("file_description")]
        public string FileDescription { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty("time_created")]
        public ulong TimeCreated { get; set; }

        [JsonProperty("time_updated")]
        public ulong TimeUpdated { get; set; }

        [JsonProperty("visibility")]
        public uint Visibility { get; set; }

        [JsonProperty("flags")]
        public ulong Flags { get; set; }

        [JsonProperty("workshop_file")]
        public bool WorkshopFile { get; set; }

        [JsonProperty("workshop_accepted")]
        public bool WorkshopAccepted { get; set; }

        [JsonProperty("show_subscribe_all")]
        public bool ShowSubscribeAll { get; set; }

        [JsonProperty("num_comments_public")]
        public ulong NumCommentsPublic { get; set; }

        [JsonProperty("banned")]
        public bool Banned { get; set; }

        [JsonProperty("ban_reason")]
        public string BanReason { get; set; }

        [JsonProperty("banner")]
        public ulong Banner { get; set; }

        [JsonProperty("can_be_deleted")]
        public bool CanBeDeleted { get; set; }

        [JsonProperty("app_name")]
        public string AppName { get; set; }

        [JsonProperty("file_type")]
        public uint FileType { get; set; }

        [JsonProperty("can_subscribe")]
        public bool CanSubscribe { get; set; }

        [JsonProperty("subscriptions")]
        public ulong Subscriptions { get; set; }

        [JsonProperty("favorited")]
        public ulong Favorited { get; set; }

        [JsonProperty("followers")]
        public ulong Followers { get; set; }

        [JsonProperty("lifetime_subscriptions")]
        public ulong LifetimeSubscriptions { get; set; }

        [JsonProperty("lifetime_favorited")]
        public ulong LifetimeFavorited { get; set; }

        [JsonProperty("lifetime_followers")]
        public ulong LifetimeFollowers { get; set; }

        [JsonProperty("lifetime_playtime")]
        public ulong LifetimePlaytime { get; set; }

        [JsonProperty("lifetime_playtime_sessions")]
        public ulong LifetimePlaytimeSessions { get; set; }

        [JsonProperty("views")]
        public ulong Views { get; set; }

        [JsonProperty("num_children")]
        public ulong NumChildren { get; set; }

        [JsonProperty("num_reports")]
        public ulong NumReports { get; set; }

        [JsonProperty("previews")]
        public List<PublishedFilePreview> Previews { get; set; }

        [JsonProperty("tags")]
        public List<PublishedFileTag> Tags { get; set; }

        [JsonProperty("vote_data")]
        public PublishedFileVoteData VoteData { get; set; }

        [JsonProperty("playtime_stats")]
        public PublishedFilePlaytimeStats PlaytimeStats { get; set; }

        [JsonProperty("language")]
        public uint Language { get; set; }

        [JsonProperty("maybe_inappropriate_sex")]
        public bool MaybeInappropriateSex { get; set; }

        [JsonProperty("maybe_inappropriate_violence")]
        public bool MaybeInappropriateViolence { get; set; }

        [JsonProperty("revision_change_number")]
        public uint RevisionChangeNumber { get; set; }

        [JsonProperty("revision")]
        public uint Revision { get; set; }

        [JsonProperty("ban_text_check_result")]
        public bool Ban_TextCheckResult { get; set; }
    }
}
