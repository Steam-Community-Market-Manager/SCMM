namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDescriptionWithActionsDTO : ItemDescriptionDTO, ICanBeInteractedWith
    {
        public ItemInteractionDTO[] Actions { get; set; }
    }
}
