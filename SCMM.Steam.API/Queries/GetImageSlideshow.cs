using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Shared.Data.Models;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Store;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
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
            var renderTitles = imageSources.Any(x => !string.IsNullOrEmpty(x.Title));

            // Hydrate the image sources (but only as many as we need to render)
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
            
            var padding = (int)Math.Ceiling(frameHeight * 0.0625f);
            var indicatorSize = (int)Math.Ceiling(frameHeight * 0.25f);
            var fontSize = (int)Math.Ceiling(24 * ((double)frameHeight / 256));
            var fontLineHeight = (fontSize + (padding * 3));
            var fontFamily = (FontFamily)null;
            if (!SystemFonts.TryFind("Segoe UI", out fontFamily) && 
                !SystemFonts.TryFind("DejaVu Sans", out fontFamily) &&
                !SystemFonts.TryFind("Noto Sans", out fontFamily) &&
                !SystemFonts.TryFind("Liberation Sans", out fontFamily))
            {
                throw new Exception($"Unable to find a suitable font. Available options are: {String.Join(", ", SystemFonts.Families.Select(x => x.Name))}");
            }
            var font = new Font(fontFamily, fontSize, FontStyle.Regular);
            var solidBlackOutlinePen = Pens.Solid(Color.FromRgba(0, 0, 0, 128), 2);
            var solidBlack = Brushes.Solid(Color.FromRgba(0, 0, 0, 255));
            var solidWhite = Brushes.Solid(Color.FromRgba(255, 255, 255, 255));
            var solidRed = Brushes.Solid(Color.FromRgba(244, 67, 54, 255));
            var solidGreen = Brushes.Solid(Color.FromRgba(76, 175, 80, 255));
            var solidBlue = Brushes.Solid(Color.FromRgba(144, 202, 249, 255));
            var transparent = new Rgba32(255, 255, 255, 0);
            
            var slideshow = new Image<Rgba32>(frameWidth, frameHeight + (renderTitles ? fontLineHeight : 0), transparent);
            var imageSourceQueue = new Queue<ImageSource>(imageSources);
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

            while (imageSourceQueue.Any())
            {
                var imageSource = imageSourceQueue.Dequeue();
                if (imageSource?.ImageData == null)
                {
                    continue;
                }

                var frame = new Image<Rgba32>(frameWidth, frameHeight + (renderTitles ? fontLineHeight : 0), transparent);
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

                if (imageSource.Badge > 1)
                {
                    var badgeText = $"{imageSource.Badge}";
                    var badgeTextSize = TextMeasurer.Measure(badgeText, new RendererOptions(font));
                    var badgeRect = new Rectangle(
                        (int)(frameWidth - badgeTextSize.Width - padding),
                        (int)(0),
                        (int)(badgeTextSize.Width + padding),
                        (int)(badgeTextSize.Height + padding)
                    );

                    var badgeIconPath = new RectangularPolygon(badgeRect);
                    frame.Mutate(ctx => ctx
                        .Fill(solidBlue, badgeIconPath)
                        .Draw(solidBlackOutlinePen, badgeIconPath)
                        .DrawText(
                            badgeText, 
                            font, 
                            solidBlack,
                            new PointF(
                                badgeRect.Left + (padding / 2),
                                badgeRect.Top + (padding / 2)
                            )
                        )
                    );
                }

                var symbolX = (frameWidth - indicatorSize - padding);
                var symbolY = (frameHeight - indicatorSize - padding);
                var symbolRect = new Rectangle(symbolX, symbolY, indicatorSize, indicatorSize);
                switch (imageSource.Symbol)
                {
                    case ImageSymbol.ChevronUp:
                        frame.Mutate(ctx => ctx
                            .FillPolygon(solidGreen, GetChevronUpPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                            .DrawPolygon(solidBlackOutlinePen, GetChevronUpPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                            .FillPolygon(solidGreen, GetChevronUpPoints(symbolX, symbolY, indicatorSize))
                            .DrawPolygon(solidBlackOutlinePen, GetChevronUpPoints(symbolX, symbolY, indicatorSize))
                        );
                        break;

                    case ImageSymbol.ChevronDown:
                        frame.Mutate(ctx => ctx
                            .FillPolygon(solidRed, GetChevronDownPoints(symbolX, symbolY, indicatorSize))
                            .DrawPolygon(solidBlackOutlinePen, GetChevronDownPoints(symbolX, symbolY, indicatorSize))
                            .FillPolygon(solidRed, GetChevronDownPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                            .DrawPolygon(solidBlackOutlinePen, GetChevronDownPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                        );
                        break;

                    case ImageSymbol.Cross:
                        var lineWidth = (indicatorSize / 6);
                        var lineFill = Pens.Solid(solidRed, lineWidth);
                        frame.Mutate(ctx => ctx
                            .DrawLines(lineFill, new PointF(symbolRect.Left, symbolRect.Top), new PointF(symbolRect.Right, symbolRect.Bottom))
                            .DrawLines(lineFill, new PointF(symbolRect.Right, symbolRect.Top), new PointF(symbolRect.Left, symbolRect.Bottom))
                        );
                        break;
                }

                if (!string.IsNullOrEmpty(imageSource.Title))
                {
                    var title = imageSource.Title;
                    var titleWidth = TextMeasurer.Measure(title, new RendererOptions(font)).Width;
                    while (titleWidth >= (frameWidth - (padding * 2)) && title.Length > 0)
                    {
                        title = title.Substring(0, title.Length - 1);
                        titleWidth = TextMeasurer.Measure(title, new RendererOptions(font)).Width;
                    }
                    if (title.Length != imageSource.Title.Length)
                    {
                        title = (title.Trim() + "…");
                        titleWidth = TextMeasurer.Measure(title, new RendererOptions(font)).Width;
                    }
                    if (!string.IsNullOrEmpty(title) && titleWidth > 0)
                    {
                        frame.Mutate(ctx => ctx
                            .DrawText(
                                title,
                                font,
                                solidWhite,
                                solidBlackOutlinePen,
                                new PointF(
                                    (frameWidth / 2) - (titleWidth / 2),
                                    imageSize + padding
                                )
                            )
                        );
                    }
                }

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

        private static PointF[] GetChevronUpPoints(float x, float y, int size)
        {
            return new PointF[]
            {
                new PointF(x + (size / 2), y + (size / 2)),
                new PointF(x, y + size),
                new PointF(x + size, y + size)
            };
        }

        private static PointF[] GetChevronDownPoints(float x, float y, int size)
        {
            return new PointF[]
            {
                new PointF(x, y + (size / 2)),
                new PointF(x + size, y  + (size / 2)),
                new PointF(x + (size / 2), y + size)
            };
        }
    }
}
