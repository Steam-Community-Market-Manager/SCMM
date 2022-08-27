namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemCollectionDTO
    {
        public string Name { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public long? BuyNowPrice { get; set; }

        public ItemDescriptionWithPriceDTO[] AcceptedItems { get; set; }

        public ItemDescriptionWithActionsDTO[] UnacceptedItems { get; set; }
    }
}
