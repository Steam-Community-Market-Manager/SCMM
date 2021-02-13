using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscordController : ControllerBase
    {
        private readonly ILogger<DiscordController> _logger;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public DiscordController(ILogger<DiscordController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }
    }
}
