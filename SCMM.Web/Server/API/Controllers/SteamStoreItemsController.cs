using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Steam;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class SteamStoreItemsController : ControllerBase
    {
        private readonly ILogger<SteamStoreItemsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public SteamStoreItemsController(ILogger<SteamStoreItemsController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<SteamStoreItemDTO> Get(string filter = null)
        {
            filter = filter?.Trim();
            return _db.SteamStoreItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Include(x => x.Description.WorkshopFile.Creator)
                .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                .Select(x => _mapper.Map<SteamStoreItemDTO>(x))
                .ToList();
        }
    }
}
