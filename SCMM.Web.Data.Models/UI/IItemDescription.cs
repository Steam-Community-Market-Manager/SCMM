namespace SCMM.Web.Data.Models.UI
{
    public interface IItemDescription
    {
        public string Name { get; }

        public string ItemType { get; }

        public string BackgroundColour { get; }

        public string ForegroundColour { get; }

        public string IconUrl { get; }
    }
}
