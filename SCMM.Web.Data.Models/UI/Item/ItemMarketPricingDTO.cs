namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemMarketPricingDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public IEnumerable<ItemMarketPriceDTO> Prices { get; set; }
    }
}
