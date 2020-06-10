namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamProfileDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string Country { get; set; }
    }
}
