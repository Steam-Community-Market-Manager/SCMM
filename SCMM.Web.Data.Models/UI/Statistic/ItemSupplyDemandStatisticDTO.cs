using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemSupplyDemandStatisticDTO : ItemDescriptionDTO
    {
        public long Supply { get; set; }

        public long Demand { get; set; }
    }
}
