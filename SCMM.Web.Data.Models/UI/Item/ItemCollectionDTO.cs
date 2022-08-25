namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemCollectionDTO
    {
        public string Name { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public long? BuyNowPrice { get; set; }

        public IList<ItemDescriptionWithPriceDTO> AcceptedItems { get; set; }

        public IList<ItemDescriptionWithActionsDTO> UnacceptedItems { get; set; }
    }
}
