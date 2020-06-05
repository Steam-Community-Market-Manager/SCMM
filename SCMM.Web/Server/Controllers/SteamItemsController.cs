using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SCMM.Web.Shared.Models.Steam;

namespace SCMM.Web.Server.Controllers
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
                .Include(x => x.App)
                .Include(x => x.Description)
                .OrderBy(x => x.Name)
                .Select(x => _mapper.Map<SteamItemDTO>(x))
                .ToList();
        }
    }
}
