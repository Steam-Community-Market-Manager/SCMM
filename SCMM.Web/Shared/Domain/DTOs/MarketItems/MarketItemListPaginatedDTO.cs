namespace SCMM.Web.Shared.Domain.DTOs.MarketItems
{
    public class MarketItemListPaginatedDTO
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }

        public MarketItemListDTO[] Items { get; set; }
    }
}
