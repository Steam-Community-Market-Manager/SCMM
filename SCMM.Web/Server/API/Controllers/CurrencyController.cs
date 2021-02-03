using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ILogger<CurrencyController> _logger;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public CurrencyController(ILogger<CurrencyController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<CurrencyListDTO> Get()
        {
            return _db.SteamCurrencies
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<CurrencyListDTO>(x))
                .ToList();
        }

        [AllowAnonymous]
        [HttpGet("withDetails")]
        public IEnumerable<CurrencyDetailedDTO> GetWithDetails()
        {
            return _db.SteamCurrencies
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<CurrencyDetailedDTO>(x))
                .ToList();
        }
    }
}
