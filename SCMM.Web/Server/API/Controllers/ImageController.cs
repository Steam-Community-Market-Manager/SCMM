using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Services;
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

        private readonly ImageService _images;

        public ImageController(ILogger<ImageController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper, ImageService images)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
            _images = images;
        }

        [AllowAnonymous]
        [HttpGet("mosaic")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBadgeMosaic([FromQuery] Guid[] imageIds = null, [FromQuery] int imageSize = 128, [FromQuery] int columns = 5, [FromQuery] int? rows = null)
        {
            imageIds = (imageIds ?? new Guid[0]);
            var images = _db.ImageData
                .AsNoTracking()
                .Where(x => imageIds.Length == 0 || imageIds.Contains(x.Id))
                .Select(x => new ImageSource()
                {
                    ImageData = x.Data,
                })
                .ToList();

            if (images?.Any() != true)
            {
                return NotFound();
            }

            var mosaic = await _images.GenerateImageMosaic(
                images,
                tileSize: imageSize,
                columns: columns,
                rows: (rows ?? Int32.MaxValue)
            );

            if (mosaic != null && mosaic.Length > 0)
            {
                return File(mosaic, "image/png");
            }
            else
            {
                return NotFound();
            }
        }
    }
}
