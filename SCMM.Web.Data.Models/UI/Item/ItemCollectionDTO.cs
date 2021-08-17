namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemCollectionDTO
    {
        public string Name { get; set; }

        public string AuthorName { get; set; }

        public string AuthorAvatarUrl { get; set; }

        public long? BuyNowPrice { get; set; }

        public IList<ItemDescriptionWithPriceDTO> Items { get; set; }
    }
}
