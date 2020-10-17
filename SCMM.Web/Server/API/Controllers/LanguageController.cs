using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Languages;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguageController : ControllerBase
    {
        private readonly ILogger<LanguageController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public LanguageController(ILogger<LanguageController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<LanguageListDTO> Get()
    {
            return _db.SteamLanguages
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<LanguageListDTO>(x))
                .ToList();
        }

        [AllowAnonymous]
        [HttpGet("withDetails")]
        public IEnumerable<LanguageDetailedDTO> GetWithDetails()
        {
            return _db.SteamLanguages
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<LanguageDetailedDTO>(x))
                .ToList();
        }
    }
}
