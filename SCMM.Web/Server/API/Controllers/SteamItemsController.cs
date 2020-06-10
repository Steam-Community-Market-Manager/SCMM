using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Steam;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SteamItemsController : ControllerBase
    {
        private readonly ILogger<SteamItemsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public SteamItemsController(ILogger<SteamItemsController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<SteamMarketItemDTO> Get(string filter = null)
        {
            filter = filter?.Trim();
            return _db.SteamMarketItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter))
                .OrderBy(x => x.Description.Name)
                .Select(x => _mapper.Map<SteamMarketItemDTO>(x))
                .ToList();
        }
    }
}
