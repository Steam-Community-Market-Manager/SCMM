namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionWorkshopFileDTO : ItemDescriptionDTO, ICanBeInteractedWith
    {
        public ItemInteractionDTO[] Actions { get; set; }
    }
}
