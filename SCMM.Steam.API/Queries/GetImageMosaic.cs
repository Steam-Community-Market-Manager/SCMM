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

        public int TileSize { get; set; } = 256;

        public int Columns { get; set; } = 3;

        public int? Rows { get; set; } = null;
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

            var columns = Math.Max(1, request.Columns);
            var rows = Math.Max(1, request.Rows ?? int.MaxValue);
            var tileSize = Math.Max(8, request.TileSize);

            var x = 0;
            var y = 0;
            var padding = (int)Math.Ceiling(tileSize * 0.0625f);
            var indicatorSize = (int)Math.Ceiling(tileSize * 0.25f);
            var fontSize = (int)Math.Ceiling(24 * ((double)tileSize / 128));
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
            var solidBlackOutlinePen = Pens.Solid(Color.FromRgba(0, 0, 0, 128), 3);
            var solidBlack = Brushes.Solid(Color.FromRgba(0, 0, 0, 255));
            var solidWhite = Brushes.Solid(Color.FromRgba(255, 255, 255, 255));
            var solidRed = Brushes.Solid(Color.FromRgba(244, 67, 54, 255));
            var solidGreen = Brushes.Solid(Color.FromRgba(76, 175, 80, 255));
            var solidBlue = Brushes.Solid(Color.FromRgba(144, 202, 249, 255));
            var imageSize = tileSize;

            var renderTitles = imageSources.Any(x => !string.IsNullOrEmpty(x.Title));
            if (renderTitles)
            {
                tileSize += fontLineHeight;
            }

            // If there are rows than we have images for, reduces the row count to the minimum required to render the images
            var minimumRowsToRenderTiles = (int)Math.Ceiling((float)tileCount / columns);
            rows = Math.Min(minimumRowsToRenderTiles, rows);

            // Hydrate the image sources (but only as many as we need to render)
            var maxTiles = Math.Min(tileCount, (columns * rows));
            imageSources = imageSources.Take(maxTiles).ToList();
            await HydrateImageData(imageSources);

            var mosaic = new Image<Rgba32>(columns * tileSize, rows * tileSize);
            var imageSourceQueue = new Queue<ImageSource>(imageSources);
            mosaic.Mutate(ctx => ctx
                .SetGraphicsOptions(new GraphicsOptions()
                {
                    Antialias = true
                })
            );

            y = 0;
            for (var r = 0; r < rows; r++)
            {
                x = 0;
                for (var c = 0; c < columns; c++)
                {
                    var imageSource = (imageSourceQueue.Any() ? imageSourceQueue.Dequeue() : null);
                    if (imageSource?.ImageData == null)
                    {
                        continue;
                    }

                    var image = Image.Load<Rgba32>(new MemoryStream(imageSource.ImageData));
                    image.Mutate(ctx => ctx
                        .Resize(imageSize, imageSize, KnownResamplers.Bicubic)
                    );
                    mosaic.Mutate(ctx => ctx
                        .DrawImage(
                            image,
                            new Point(
                                x + ((Math.Max(2, tileSize - imageSize) / 2) - 1), 
                                y
                            ),
                            ctx.GetGraphicsOptions()
                        )
                    );

                    if (imageSource.Badge > 1)
                    {
                        var badgeText = $"{imageSource.Badge}";
                        var badgeTextSize = TextMeasurer.Measure(badgeText, new RendererOptions(font));
                        var badgeRect = new Rectangle(
                            (int)(x + tileSize - badgeTextSize.Width - (padding / 2)),
                            (int)(y + (padding / 2)),
                            (int)(badgeTextSize.Width + padding),
                            (int)(badgeTextSize.Height + padding)
                        );

                        var badgeIconPath = new RectangularPolygon(badgeRect);
                        mosaic.Mutate(ctx => ctx
                            .Fill(solidBlue, badgeIconPath)
                            .Draw(solidBlackOutlinePen, badgeIconPath)
                            .DrawText(
                                badgeText, 
                                font, 
                                solidBlack,
                                new PointF(
                                    badgeRect.Left + (padding / 2),
                                    badgeRect.Top 
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
                        var titleWidth = TextMeasurer.Measure(title, new RendererOptions(font)).Width;
                        while (titleWidth >= imageSize && title.Length > 0)
                        {
                            title = title.Substring(0, title.Length - 1);
                            titleWidth = TextMeasurer.Measure(title, new RendererOptions(font)).Width;
                        }
                        if (title.Length != imageSource.Title.Length)
                        {
                            title += "…";
                        }
                        if (!string.IsNullOrEmpty(title) && titleWidth > 0)
                        {
                            mosaic.Mutate(ctx => ctx
                                .DrawText(
                                    title,
                                    font,
                                    solidWhite,
                                    new PointF(
                                        x + ((Math.Max(2, tileSize - titleWidth) / 2) - 1),
                                        y + imageSize + padding
                                    )
                                )
                            );
                        }
                    }
                    x += tileSize;
                }
                y += tileSize;
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
