using SCMM.Shared.Data.Models;
using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.App
{
    public class AppDTO : IApp
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }

        public string TertiaryColor { get; set; }

        public string SurfaceColor { get; set; }

        public string BackgroundColor { get; set; }
    }
}
