using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public LanguageController(ILogger<LanguageController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<LanguageListDTO> Get()
        {
            return _db.SteamLanguages
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<LanguageListDTO>(x))
                .ToList();
        }

        [AllowAnonymous]
        [HttpGet("withDetails")]
        public IEnumerable<LanguageDetailedDTO> GetWithDetails()
        {
            return _db.SteamLanguages
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<LanguageDetailedDTO>(x))
                .ToList();
        }
    }
}
