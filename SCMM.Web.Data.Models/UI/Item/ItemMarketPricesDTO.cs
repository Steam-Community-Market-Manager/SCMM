namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemMarketPricesDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string Name { get; set; }

        public ItemBasicMarketPriceDTO[] Prices { get; set; }
    }
}
