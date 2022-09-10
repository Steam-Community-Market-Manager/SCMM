using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI
{
    public interface ICanBeInteractedWith
    {
        public ItemInteractionDTO[] Actions { get; }
    }
}
