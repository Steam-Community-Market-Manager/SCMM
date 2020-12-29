using CommandQuery;
using System;

namespace SCMM.Web.Server.Services.Commands.FetchAndCreateSteamProfile
{
    public class ResolveSteamIdRequest : IQuery<ResolveSteamIdResponse>
    {
        public string Id { get; set; }
    }
}
