namespace SCMM.Web.Data.Models.UI
{
    public interface ICanBeOwned
    {
        public long? Subscriptions { get; }

        public long? SupplyTotalEstimated { get; }
    }
}
