
namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemFilterDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public int Size { get; set; }

        public string Icon { get; set; }

        public Dictionary<string, string> Options { get; set; }

        public bool IsEnabled { get; set; }
    }
}
