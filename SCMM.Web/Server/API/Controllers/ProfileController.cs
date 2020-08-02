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
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
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

        [HttpGet("me")]
        public async Task<ProfileDetailedDTO> GetMyState()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();

                var language = Request.Language();
                if (language == null)
                {
                    throw new Exception($"Language '{Request.LanguageId()}' is not supported");
                }

                var currency = Request.Currency();
                if (currency == null)
                {
                    throw new Exception($"Currency '{Request.CurrencyId()}' is not supported");
                }

                var profileId = Request.ProfileId();
                var profileState = new ProfileDetailedDTO();
                if (!String.IsNullOrEmpty(profileId))
                {
                    var profile = await db.SteamProfiles
                        .Include(x => x.Language)
                        .Include(x => x.Currency)
                        .FirstOrDefaultAsync(
                            x => x.SteamId == profileId || x.ProfileId == profileId
                        );

                    if (profile == null)
                    {
                        throw new Exception($"Profile with Steam ID '{profileId}' was not found");
                    }

                    _mapper.Map(profile, profileState);
                }

                profileState.Language = language;
                profileState.Currency = currency;
                return profileState;
            }
        }

        [HttpPut("me")]
        public async void SetMyState([FromBody] UpdateProfileStateCommand command)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var steamService = scope.ServiceProvider.GetService<SteamService>();
                var profile = await steamService.AddOrUpdateSteamProfile(Request.ProfileId());
                if (profile == null)
                {
                    throw new Exception($"Profile with Steam ID '{Request.ProfileId()}' was not found");
                }

                profile.Country = (command.Country ?? profile.Country);
                profile.Language = await db.SteamLanguages.FirstOrDefaultAsync(x => x.Name == command.Language);
                profile.Currency = await db.SteamCurrencies.FirstOrDefaultAsync(x => x.Name == command.Currency);
                await db.SaveChangesAsync();
            }
        }
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
