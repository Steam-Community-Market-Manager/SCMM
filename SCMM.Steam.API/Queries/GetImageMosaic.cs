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

namespace SCMM.Steam.API.Queries
{
    public class GetImageMosaicRequest : IQuery<GetImageMosaicResponse>
    {
        public IEnumerable<ImageSource> ImageSources { get; set; }

        public int ImageSize { get; set; } = 256;

        public int ImageColumns { get; set; } = 3;

        public int? ImageRows { get; set; }
    }

    public class GetImageMosaicResponse
    {
        public byte[] Data { get; set; }

        public string MimeType { get; set; }
    }

    public class GetImageMosaic :
        IQueryHandler<GetImageMosaicRequest, GetImageMosaicResponse>
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;

        public GetImageMosaic(SteamDbContext db, ICommandProcessor commandProcessor)
        {
            _db = db;
            _commandProcessor = commandProcessor;
        }

        public async Task<GetImageMosaicResponse> HandleAsync(GetImageMosaicRequest request)
        {
            var imageSources = request.ImageSources.ToList();
            var tileCount = imageSources.Count;
            if (tileCount < 1)
            {
                return null;
            }

            var tileColumns = Math.Max(1, request.ImageColumns);
            var tileRows = Math.Max(1, request.ImageRows ?? int.MaxValue);
            var tileSize = Math.Max(32, request.ImageSize);
            var renderTitles = imageSources.Any(x => !string.IsNullOrEmpty(x.Title));

            var x = 0;
            var y = 0;
            var padding = (int)Math.Ceiling(tileSize * 0.0625f);
            var indicatorSize = (int)Math.Ceiling(tileSize * 0.25f);
            var fontFamily = (FontFamily)null;
            if (!SystemFonts.TryFind("Segoe UI", out fontFamily) && 
                !SystemFonts.TryFind("DejaVu Sans", out fontFamily) &&
                !SystemFonts.TryFind("Noto Sans", out fontFamily) &&
                !SystemFonts.TryFind("Liberation Sans", out fontFamily))
            {
                throw new Exception($"Unable to find a suitable font. Available options are: {String.Join(", ", SystemFonts.Families.Select(x => x.Name))}");
            }
            var badgeFont = new Font(fontFamily, (int)Math.Ceiling(16 * ((double)tileSize / 128)), FontStyle.Regular);
            var titleFont = new Font(fontFamily, (int)Math.Ceiling(24 * ((double)tileSize / 128)), FontStyle.Regular);
            var titleLineHeight = (renderTitles ? ((int)titleFont.Size + (padding * 2)) : 0);

            var solidBlackOutlinePen = Pens.Solid(Color.FromRgba(0, 0, 0, 128), 2);
            var solidBlack = Brushes.Solid(Color.FromRgba(0, 0, 0, 255));
            var solidWhite = Brushes.Solid(Color.FromRgba(255, 255, 255, 255));
            var solidRed = Brushes.Solid(Color.FromRgba(244, 67, 54, 255));
            var solidGreen = Brushes.Solid(Color.FromRgba(76, 175, 80, 255));
            var solidBlue = Brushes.Solid(Color.FromRgba(144, 202, 249, 255));
            var transparent = new Rgba32(255, 255, 255, 0);

            // If there are rows than we have images for, reduces the row count to the minimum required to render the images
            var minimumRowsToRenderTiles = (int)Math.Ceiling((float)tileCount / tileColumns);
            tileRows = Math.Min(minimumRowsToRenderTiles, tileRows);

            // Hydrate the image sources (but only as many as we need to render)
            var maxTiles = Math.Min(tileCount, (tileColumns * tileRows));
            imageSources = imageSources.Take(maxTiles).ToList();
            await HydrateImageData(imageSources);

            var mosaic = new Image<Rgba32>(tileColumns * tileSize, tileRows * (tileSize + titleLineHeight), transparent);
            var mosaicMetadata = mosaic.Metadata.GetPngMetadata();
            mosaicMetadata.HasTransparency = true;

            var imageSourceQueue = new Queue<ImageSource>(imageSources);
            mosaic.Mutate(ctx => ctx
                .BackgroundColor(transparent)
                .SetGraphicsOptions(new GraphicsOptions()
                {
                    Antialias = true
                })
            );

            y = 0;
            for (var r = 0; r < tileRows; r++)
            {
                x = 0;
                for (var c = 0; c < tileColumns; c++)
                {
                    var imageSource = (imageSourceQueue.Any() ? imageSourceQueue.Dequeue() : null);
                    if (imageSource?.ImageData == null)
                    {
                        continue;
                    }

                    var image = Image.Load<Rgba32>(new MemoryStream(imageSource.ImageData));
                    image.Mutate(ctx => ctx
                        .BackgroundColor(transparent)
                        .Resize(tileSize, tileSize, KnownResamplers.Lanczos5)
                    );
                    mosaic.Mutate(ctx => ctx
                        .BackgroundColor(transparent)
                        .DrawImage(
                            image,
                            new Point(x, y),
                            ctx.GetGraphicsOptions()
                        )
                    );

                    if (imageSource.Badge > 1)
                    {
                        var badgeText = $"{imageSource.Badge}";
                        var badgeTextSize = TextMeasurer.Measure(badgeText, new RendererOptions(badgeFont));
                        var badgeRect = new Rectangle(
                            (int)(x + tileSize - badgeTextSize.Width - padding),
                            (int)(y),
                            (int)(badgeTextSize.Width + padding),
                            (int)(badgeTextSize.Height + padding)
                        );

                        var badgeIconPath = new RectangularPolygon(badgeRect);
                        mosaic.Mutate(ctx => ctx
                            .Fill(solidBlue, badgeIconPath)
                            .Draw(solidBlackOutlinePen, badgeIconPath)
                            .DrawText(
                                badgeText,
                                badgeFont, 
                                solidBlack,
                                new PointF(
                                    badgeRect.Left + (padding / 2),
                                    badgeRect.Top + (padding / 2)
                                )
                            )
                        );
                    }

                    var symbolX = (x + tileSize - indicatorSize - padding);
                    var symbolY = (y + tileSize - indicatorSize - padding);
                    var symbolRect = new Rectangle(symbolX, symbolY, indicatorSize, indicatorSize);
                    switch (imageSource.Symbol)
                    {
                        case ImageSymbol.ChevronUp:
                            mosaic.Mutate(ctx => ctx
                                .FillPolygon(solidGreen, GetChevronUpPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                                .DrawPolygon(solidBlackOutlinePen, GetChevronUpPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                                .FillPolygon(solidGreen, GetChevronUpPoints(symbolX, symbolY, indicatorSize))
                                .DrawPolygon(solidBlackOutlinePen, GetChevronUpPoints(symbolX, symbolY, indicatorSize))
                            );
                            break;

                        case ImageSymbol.ChevronDown:
                            mosaic.Mutate(ctx => ctx
                                .FillPolygon(solidRed, GetChevronDownPoints(symbolX, symbolY, indicatorSize))
                                .DrawPolygon(solidBlackOutlinePen, GetChevronDownPoints(symbolX, symbolY, indicatorSize))
                                .FillPolygon(solidRed, GetChevronDownPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                                .DrawPolygon(solidBlackOutlinePen, GetChevronDownPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize))
                            );
                            break;

                        case ImageSymbol.Cross:
                            var lineWidth = (indicatorSize / 6);
                            var lineFill = Pens.Solid(solidRed, lineWidth);
                            mosaic.Mutate(ctx => ctx
                                .DrawLines(lineFill, new PointF(symbolRect.Left, symbolRect.Top), new PointF(symbolRect.Right, symbolRect.Bottom))
                                .DrawLines(lineFill, new PointF(symbolRect.Right, symbolRect.Top), new PointF(symbolRect.Left, symbolRect.Bottom))
                            );
                            break;
                    }

                    if (!string.IsNullOrEmpty(imageSource.Title))
                    {
                        var title = imageSource.Title;
                        var titleSize = TextMeasurer.Measure(title, new RendererOptions(titleFont));
                        while (titleSize.Width >= (tileSize - (padding * 2)) && title.Length > 0)
                        {
                            title = title.Substring(0, title.Length - 1);
                            titleSize = TextMeasurer.Measure(title, new RendererOptions(titleFont));
                        }
                        if (title.Length != imageSource.Title.Length)
                        {
                            title = (title.Trim() + "…");
                            titleSize = TextMeasurer.Measure(title, new RendererOptions(titleFont));
                        }
                        if (!string.IsNullOrEmpty(title) && titleSize.Width > 0)
                        {
                            mosaic.Mutate(ctx => ctx
                                .DrawText(
                                    title,
                                    titleFont,
                                    solidWhite,
                                    solidBlackOutlinePen,
                                    new PointF(
                                        x + (tileSize / 2) - (titleSize.Width / 2),
                                        y + tileSize + padding
                                    )
                                )
                            );
                        }
                    }
                    x += tileSize;
                }
                y += tileSize + (renderTitles ? titleLineHeight : 0);
            }

            using var mosaicStream = new MemoryStream();
            await mosaic.SaveAsPngAsync(mosaicStream);
            var mosaicRaw = mosaicStream.ToArray();
            return new GetImageMosaicResponse
            {
                Data = mosaicRaw,
                MimeType = "image/png"
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
