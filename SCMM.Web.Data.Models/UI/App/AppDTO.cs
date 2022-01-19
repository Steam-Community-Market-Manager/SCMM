using SCMM.Shared.Data.Models;

namespace SCMM.Web.Data.Models.UI.App
{
    public class AppDTO : IApp
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string BackgroundColor { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }

        public string Subdomain { get; set; }
    }
}
