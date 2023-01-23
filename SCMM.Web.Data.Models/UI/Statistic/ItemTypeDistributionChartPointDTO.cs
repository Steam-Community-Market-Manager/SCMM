using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemTypeDistributionChartPointDTO
    {
        public string ItemType { get; set; }

        public long Submitted { get; set; }

        public long Accepted { get; set; }

        public long Total => (Submitted + Accepted);
    }
}
