using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Domain;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
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

        [HttpGet("steam/{steamId}/summary")]
        public async Task<ProfileSummaryDTO> GetSteamIdSummary([FromRoute] string steamId)
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

                return _mapper.Map<ProfileSummaryDTO>(profile);
            }
        }
    }
}
