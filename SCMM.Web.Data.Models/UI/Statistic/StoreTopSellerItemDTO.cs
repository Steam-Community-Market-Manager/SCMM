namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class StoreTopSellerItemDTO
    {
        public string Name { get; set; }

        public string IconAccentColour { get; set; }

        public string IconUrl { get; set; }

        public int Position { get; set; }

        public List<StoreTopSellerPositionChartPointDTO> PositionChanges { get; set; }
    }
}
