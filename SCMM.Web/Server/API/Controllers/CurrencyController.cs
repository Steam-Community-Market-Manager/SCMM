using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ILogger<StoreItemsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public CurrencyController(ILogger<StoreItemsController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<CurrencyDetailsDTO> Get()
        {
            return _db.SteamCurrencies
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<CurrencyDetailsDTO>(x))
                .ToList();
        }
    }
}
