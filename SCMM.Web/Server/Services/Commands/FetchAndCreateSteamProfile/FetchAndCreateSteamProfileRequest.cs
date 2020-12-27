using CommandQuery;
using System;

namespace SCMM.Web.Server.Services.Commands.FetchAndCreateSteamProfile
{
    public class FetchAndCreateSteamProfileRequest : ICommand<FetchAndCreateSteamProfileResponse>
    {
        public string Id { get; set; }
    }
}
