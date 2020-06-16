using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class MarketItemsController : ControllerBase
    {
        private readonly ILogger<MarketItemsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public MarketItemsController(ILogger<MarketItemsController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<MarketItemListDTO> Get([FromQuery] string filter = null)
        {
            filter = filter?.Trim();
            return _db.SteamMarketItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter))
                .OrderBy(x => x.Description.Name)
                .Take(100)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("{id}")]
        public MarketItemDetailDTO Get(Guid id)
        {
            return _db.SteamMarketItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Id == id)
                .Select(x => _mapper.Map<MarketItemDetailDTO>(x))
                .SingleOrDefault();
        }
    }
}
