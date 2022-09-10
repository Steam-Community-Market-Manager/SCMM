namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class StoreTopSellerItemDTO
    {
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string DominantColour { get; set; }

        public int Position { get; set; }

        public List<StoreTopSellerPositionChartPointDTO> PositionChanges { get; set; }
    }
}
