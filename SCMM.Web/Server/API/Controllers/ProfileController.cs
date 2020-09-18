using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.API.Controllers.Extensions;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public ProfileController(ILogger<ProfileController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("me")]
        public async Task<ProfileDetailedDTO> GetMyProfile()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                
                // If the user is authenticated, use their database profile
                if (User.Identity.IsAuthenticated)
                {
                    var profileId = User.Id();
                    var profile = await db.SteamProfiles
                        .Include(x => x.Language)
                        .Include(x => x.Currency)
                        .FirstOrDefaultAsync(x => x.Id == profileId);

                    return _mapper.Map<ProfileDetailedDTO>(profile);
                }

                // Else, use a transient guest profile
                else
                {
                    return new ProfileDetailedDTO()
                    {
                        Name = "Guest",
                        Language = this.Language(),
                        Currency = this.Currency()
                    };
                }
            }
        }

        [Authorize]
        [HttpPut("me")]
        public async void SetMyProfile([FromBody] UpdateProfileCommand command)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var profileId = User.Id();
                var profile = await db.SteamProfiles
                    .Include(x => x.Language)
                    .Include(x => x.Currency)
                    .FirstOrDefaultAsync(x => x.Id == profileId);

                if (profile == null)
                {
                    throw new Exception($"Profile with Steam ID '{User.SteamId()}' was not found");
                }

                if (!String.IsNullOrEmpty(command.Country))
                {
                    profile.Country = command.Country;
                }
                if (!String.IsNullOrEmpty(command.Language))
                {
                    profile.Language = await db.SteamLanguages.FirstOrDefaultAsync(x => x.Name == command.Language);
                }
                if (!String.IsNullOrEmpty(command.Currency))
                {
                    profile.Currency = await db.SteamCurrencies.FirstOrDefaultAsync(x => x.Name == command.Currency);
                }

                db.SaveChanges();
            }
        }

        [AllowAnonymous]
        [HttpGet("steam/{steamId}/summary")]
        public async Task<ProfileDTO> GetSteamIdSummary([FromRoute] string steamId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<SteamService>();
                var profile = await service.AddOrUpdateSteamProfile(steamId);
                if (profile == null)
                {
                    _logger.LogError($"Profile with SteamID '{steamId}' was not found");
                    return null;
                }

                return _mapper.Map<ProfileDTO>(profile);
            }
        }
    }
}
