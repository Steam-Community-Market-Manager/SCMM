using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Shared.Data.Models;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Store;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Gif;

namespace SCMM.Steam.API.Queries
{
    public class GetImageSlideshowRequest : IQuery<GetImageSlideshowResponse>
    {
        public IEnumerable<ImageSource> ImageSources { get; set; }

        public int ImageSize { get; set; } = 512;

        public int ImageFrameDelay { get; set; } = 300;

        public int? MaxImages { get; set; }
    }

    public class GetImageSlideshowResponse
    {
        public byte[] Data { get; set; }

        public string MimeType { get; set; }
    }

    public class GetImageSlideshow :
        IQueryHandler<GetImageSlideshowRequest, GetImageSlideshowResponse>
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;

        public GetImageSlideshow(SteamDbContext db, ICommandProcessor commandProcessor)
        {
            _db = db;
            _commandProcessor = commandProcessor;
        }

        public async Task<GetImageSlideshowResponse> HandleAsync(GetImageSlideshowRequest request)
        {
            var imageSources = request.ImageSources.ToList();
            var tileCount = imageSources.Count;
            if (tileCount < 1)
            {
                return null;
            }

            var imageSize = Math.Max(32, request.ImageSize);
            var imageFrameDelay = Math.Max(100, request.ImageFrameDelay);
            var maxFrames = Math.Min(tileCount, request.MaxImages ?? int.MaxValue);
            
            imageSources = imageSources.Take(maxFrames).ToList();
            await HydrateImageData(imageSources);
            if (!imageSources.Any())
            {
                return null;
            }

            var imageInfo = Image.Identify(imageSources.First().ImageData);
            var imageRatio = ((decimal)imageInfo.Width / (decimal)imageInfo.Height);
            var frameWidth = imageSize;
            var frameHeight = (int) Math.Ceiling(imageSize / imageRatio);
            var transparent = new Rgba32(255, 255, 255, 0);
            
            var slideshow = new Image<Rgba32>(frameWidth, frameHeight, transparent);
            slideshow.Mutate(ctx => ctx
                .BackgroundColor(transparent)
                .SetGraphicsOptions(new GraphicsOptions()
                {
                    Antialias = true
                })
            );

            var slideshowMetadata = slideshow.Metadata.GetGifMetadata();
            slideshowMetadata.ColorTableMode = GifColorTableMode.Local;

            var rootFrameMetadata = slideshow.Frames.RootFrame.Metadata.GetGifMetadata();
            rootFrameMetadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            rootFrameMetadata.FrameDelay = 0;

            foreach (var imageSource in imageSources.Where(x => x.ImageData != null))
            {
                var frame = new Image<Rgba32>(frameWidth, frameHeight, transparent);
                var image = Image.Load<Rgba32>(new MemoryStream(imageSource.ImageData));
                image.Mutate(ctx => ctx
                    .BackgroundColor(transparent)
                    .Resize(frameWidth, 0, KnownResamplers.Lanczos5)
                );
                frame.Mutate(ctx => ctx
                    .BackgroundColor(transparent)
                    .DrawImage(
                        image,
                        new Point(0, 0),
                        ctx.GetGraphicsOptions()
                    )
                );

                var slideshowFrame = slideshow.Frames.AddFrame(frame.Frames.RootFrame);
                var frameMetadata = slideshowFrame.Metadata.GetGifMetadata();
                frameMetadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
                frameMetadata.FrameDelay = imageFrameDelay;
            }

            using var mosaicStream = new MemoryStream();
            await slideshow.SaveAsGifAsync(mosaicStream);
            var mosaicRaw = mosaicStream.ToArray();
            return new GetImageSlideshowResponse
            {
                Data = mosaicRaw,
                MimeType = "image/gif"
            };
        }

        // TODO: Consolidate code somewhere else
        private async Task HydrateImageData(IEnumerable<ImageSource> imageSources)
        {
            // Check only images that are missing image data
            var missingImages = imageSources
                .Where(x => !string.IsNullOrEmpty(x.ImageUrl))
                .Where(x => x.ImageData == null)
                .ToList();
            if (!missingImages.Any())
            {
                return;
            }

            // Check the first-level cache (memory) for missing image data
            foreach (var imageSource in missingImages.ToList())
            {
                if (Cache.TryGetValue(imageSource.ImageUrl, out byte[] imageSourceData))
                {
                    imageSource.ImageData = imageSourceData;
                    missingImages.Remove(imageSource);
                }
            }
            if (!missingImages.Any())
            {
                return;
            }

            // Check the second-level cache (database) for missing image data
            var missingImageUrls = missingImages.Select(x => x.ImageUrl).ToList();
            var missingImageData = await _db.FileData.AsNoTracking().Where(x => missingImageUrls.Contains(x.Source)).ToListAsync();
            if (missingImageData.Any())
            {
                foreach (var imageSource in missingImages.ToList())
                {
                    var imageData = missingImageData.FirstOrDefault(x => x.Source == imageSource.ImageUrl);
                    if (imageData != null)
                    {
                        Cache.Set(imageData.Source, imageData.Data);
                        imageSource.ImageData = imageData.Data;
                        missingImages.Remove(imageSource);
                    }
                }
            }
            if (!missingImages.Any())
            {
                return;
            }

            // Fetch all remaining images directly from their source
            foreach (var imageSource in missingImages)
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportFileDataRequest()
                {
                    Url = imageSource.ImageUrl,
                    UseExisting = false, // we've already checked, it doesn't exist
                });
                if (importedImage?.File?.Data != null)
                {
                    Cache.Set(imageSource.ImageUrl, importedImage.File.Data);
                    imageSource.ImageData = importedImage.File.Data;
                }
            }
        }
    }
}
