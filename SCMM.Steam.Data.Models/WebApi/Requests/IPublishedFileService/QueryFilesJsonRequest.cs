namespace SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService
{
    public class QueryFilesJsonRequest : SteamRequest
    {
        public const int QueryTypeRankedByTextSearch = 12;

        public string Key { get; set; }

        public int QueryType { get; set; } = QueryTypeRankedByTextSearch;

        public uint Page { get; set; } = 0;

        public uint NumPerPage { get; set; } = 10;

        public ulong AppId { get; set; }

        public string SearchText { get; set; }

        public bool ReturnVoteData { get; set; }

        public bool ReturnTags { get; set; }

        public bool ReturnKVTags { get; set; }

        public bool ReturnPreviews { get; set; }

        public bool ReturnChildren { get; set; }

        public bool ReturnShortDescription { get; set; }

        public bool ReturnForSaleData { get; set; }

        public bool ReturnMetadata { get; set; }

        public bool ReturnPlaytimeStats { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/IPublishedFileService/QueryFiles/v1/?key={Uri.EscapeDataString(Key)}&query_type={QueryType}&page={Page}&numperpage={NumPerPage}&appid={AppId}&search_text={Uri.EscapeDataString(SearchText)}&return_vote_data={(ReturnVoteData ? 1 : 0)}&return_tags={(ReturnTags ? 1 : 0)}&return_kv_tags={(ReturnKVTags ? 1 : 0)}&return_previews={(ReturnPreviews ? 1 : 0)}&return_children={(ReturnChildren ? 1 : 0)}&return_short_description={(ReturnShortDescription ? 1 : 0)}&return_for_sale_data={(ReturnForSaleData ? 1 : 0)}&return_metadata={(ReturnMetadata ? 1 : 0)}&return_playtime_stats={(ReturnPlaytimeStats ? 1 : 0)}"
        );
    }
}
