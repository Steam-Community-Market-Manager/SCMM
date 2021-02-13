using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public ImageController(ILogger<ImageController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("{imageId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetImage(Guid imageId)
        {
            var image = _db.ImageData.Find(imageId);
            if (image != null && image.Data?.Length > 0)
            {
                return File(image.Data, image.MimeType);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
