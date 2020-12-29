using System;

namespace SCMM.Web.Shared.Domain.DTOs.Profiles
{
    public class ProfileDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }
    }
}
