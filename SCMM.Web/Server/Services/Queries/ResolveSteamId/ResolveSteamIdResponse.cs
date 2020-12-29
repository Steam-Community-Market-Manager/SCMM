using SCMM.Web.Shared.Domain.DTOs.Profiles;
using System;

namespace SCMM.Web.Server.Services.Commands.FetchAndCreateSteamProfile
{
    public class ResolveSteamIdResponse
    {
        public Guid? Id { get; set; }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public bool Exists => (Id != null && Id != Guid.Empty);
    }
}
