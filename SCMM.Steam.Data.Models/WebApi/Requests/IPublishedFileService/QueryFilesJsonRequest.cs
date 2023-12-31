﻿namespace SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService
{
    /// <summary>
    /// https://steamapi.xpaw.me/#IPublishedFileService/QueryFiles
    /// </summary>
    public class QueryFilesJsonRequest : SteamRequest
    {
        // https://partner.steamgames.com/doc/webapi/IPublishedFileService#EPublishedFileQueryType
        public const int QueryTypeAcceptedForGameRankedByAcceptanceDate = 2;
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

        public bool ReturnDetails { get; set; }

        public bool ReturnReactions { get; set; }

        public bool StripDescriptionBBCode { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/IPublishedFileService/QueryFiles/v1/?key={Uri.EscapeDataString(Key)}&query_type={QueryType}&page={Page}&numperpage={NumPerPage}&appid={AppId}&search_text={Uri.EscapeDataString(SearchText ?? String.Empty)}&return_vote_data={(ReturnVoteData ? 1 : 0)}&return_tags={(ReturnTags ? 1 : 0)}&return_kv_tags={(ReturnKVTags ? 1 : 0)}&return_previews={(ReturnPreviews ? 1 : 0)}&return_children={(ReturnChildren ? 1 : 0)}&return_short_description={(ReturnShortDescription ? 1 : 0)}&return_for_sale_data={(ReturnForSaleData ? 1 : 0)}&return_metadata={(ReturnMetadata ? 1 : 0)}&return_playtime_stats={(ReturnPlaytimeStats ? 1 : 0)}&return_details={(ReturnDetails ? 1 : 0)}&return_reactions={(ReturnReactions ? 1 : 0)}&strip_description_bbcode={(StripDescriptionBBCode ? 1 : 0)}"
        );
    }
}
