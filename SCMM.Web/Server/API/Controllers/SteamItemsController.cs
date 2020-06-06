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
        public IEnumerable<SteamItemDTO> Get()
        {
            return _db.SteamItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<SteamItemDTO>(x))
                .ToList();
        }
    }
}
