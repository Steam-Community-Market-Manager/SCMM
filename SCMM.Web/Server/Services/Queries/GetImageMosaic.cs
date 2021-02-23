using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Services.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Queries
{
    public class GetImageMosaicRequest : IQuery<GetImageMosaicResponse>
    {
        public IEnumerable<ImageSource> ImageSources { get; set; }

        public int TileSize { get; set; } = 256;

        public int Columns { get; set; } = 3;

        public int? Rows { get; set; } = null;
    }

    public class GetTradeImageMosaicRequest : IQuery<GetImageMosaicResponse>
    {
        public IEnumerable<ImageSource> HaveImageSources { get; set; }

        public IEnumerable<ImageSource> WantImageSources { get; set; }

        public int TileSize { get; set; } = 256;

        public int Columns { get; set; } = 3;
    }

    public class GetImageMosaicResponse
    {
        public byte[] Data { get; set; }

        public string MimeType { get; set; }
    }

    public class GetImageMosaic : 
        IQueryHandler<GetImageMosaicRequest, GetImageMosaicResponse>,
        IQueryHandler<GetTradeImageMosaicRequest, GetImageMosaicResponse>
    {
        private static IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IMapper _mapper;

        public GetImageMosaic(ScmmDbContext db, ICommandProcessor commandProcessor, IMapper mapper)
        {
            _db = db;
            _commandProcessor = commandProcessor;
            _mapper = mapper;
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
            var rows = Math.Max(1, request.Rows ?? Int32.MaxValue);
            var tileSize = Math.Max(8, request.TileSize);

            var x = 0;
            var y = 0;
            var padding = (int)Math.Ceiling(tileSize * 0.0625f);
            var indicatorSize = (int)Math.Ceiling(tileSize * 0.25f);
            var fontSize = 24;
            var fontLineHeight = (fontSize + (padding * 3));
            var fontFamily = new FontFamily(GenericFontFamilies.SansSerif);
            var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            var solidBlackOutlinePen = new Pen(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), 3);
            var solidBlack = new SolidBrush(Color.FromArgb(255, 0, 0, 0));
            var solidWhite = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            var solidRed = new SolidBrush(Color.FromArgb(255, 244, 67, 54));
            var solidGreen = new SolidBrush(Color.FromArgb(255, 76, 175, 80));
            var solidBlue = new SolidBrush(Color.FromArgb(255, 144, 202, 249));
            var imageSize = tileSize;

            var renderTitles = imageSources.Any(x => !String.IsNullOrEmpty(x.Title));
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

            var mosaic = new Bitmap(columns * tileSize, rows * tileSize);
            var imageSourceQueue = new Queue<ImageSource>(imageSources);
            using (var graphics = Graphics.FromImage(mosaic))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                y = 0;
                for (int r = 0; r < rows; r++)
                {
                    x = 0;
                    for (int c = 0; c < columns; c++)
                    {
                        var imageSource = (imageSourceQueue.Any() ? imageSourceQueue.Dequeue() : null);
                        if (imageSource?.ImageData == null)
                        {
                            continue;
                        }

                        var image = Image.FromStream(new MemoryStream(imageSource.ImageData));
                        graphics.DrawImage(
                            image,
                            x + ((Math.Max(2, tileSize - imageSize) / 2) - 1),
                            y,
                            imageSize,
                            imageSize
                        );

                        if (imageSource.Badge > 1)
                        {
                            var badge = $"{imageSource.Badge}";
                            var badgeWidth = (int) Math.Max(indicatorSize, graphics.MeasureString(badge, font).Width + padding);
                            var badgeHeight = indicatorSize;
                            var badgeRect = new Rectangle(
                                x + tileSize - badgeWidth - padding,
                                y + padding,
                                badgeWidth,
                                badgeHeight
                            );

                            using (var badgePath = GetRoundedRectPath(badgeRect, badgeHeight / 2))
                            {
                                graphics.FillPath(
                                    solidBlue,
                                    badgePath
                                );
                                graphics.DrawPath(
                                    solidBlackOutlinePen,
                                    badgePath
                                );
                            }

                            graphics.DrawString(
                                badge,
                                font,
                                solidBlack,
                                new PointF(
                                    badgeRect.Left + (padding / 1.5f),
                                    badgeRect.Top + (padding / 3)
                                )
                            );
                        }

                        var symbolX = (x + tileSize - indicatorSize - padding);
                        var symbolY = (y + tileSize - indicatorSize - padding);
                        var symbolRect = new Rectangle(symbolX, symbolY, indicatorSize, indicatorSize);
                        switch (imageSource.Symbol)
                        {
                            case ImageSymbol.ChevronUp:
                                graphics.FillPolygon(
                                    solidGreen,
                                    GetChevronUpPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize)
                                );
                                graphics.DrawPolygon(
                                    solidBlackOutlinePen,
                                    GetChevronUpPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize)
                                );
                                graphics.FillPolygon(
                                    solidGreen,
                                    GetChevronUpPoints(symbolX, symbolY, indicatorSize)
                                );
                                graphics.DrawPolygon(
                                    solidBlackOutlinePen,
                                    GetChevronUpPoints(symbolX, symbolY, indicatorSize)
                                );
                                break;

                            case ImageSymbol.ChevronDown:
                                graphics.FillPolygon(
                                    solidRed,
                                    GetChevronDownPoints(symbolX, symbolY, indicatorSize)
                                );
                                graphics.DrawPolygon(
                                    solidBlackOutlinePen,
                                    GetChevronDownPoints(symbolX, symbolY, indicatorSize)
                                );
                                graphics.FillPolygon(
                                    solidRed,
                                    GetChevronDownPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize)
                                );
                                graphics.DrawPolygon(
                                    solidBlackOutlinePen,
                                    GetChevronDownPoints(symbolX, symbolY - (indicatorSize / 4), indicatorSize)
                                );
                                break;

                            case ImageSymbol.Cross:
                                var lineWidth = (indicatorSize / 6);
                                var lineFill = new Pen(solidRed, lineWidth);
                                graphics.DrawLine(lineFill, symbolRect.Left, symbolRect.Top, symbolRect.Right, symbolRect.Bottom);
                                graphics.DrawLine(lineFill, symbolRect.Right, symbolRect.Top, symbolRect.Left, symbolRect.Bottom);
                                break;
                        }

                        if (!string.IsNullOrEmpty(imageSource.Title))
                        {
                            var title = imageSource.Title;
                            var titleWidth = graphics.MeasureString(title, font).Width;
                            while (titleWidth >= imageSize && title.Length > 0)
                            {
                                title = title.Substring(0, title.Length - 1);
                                titleWidth = graphics.MeasureString(title, font).Width;
                            }
                            if (title.Length != imageSource.Title.Length)
                            {
                                title += "…";
                            }
                            if (!string.IsNullOrEmpty(title) && titleWidth > 0)
                            {
                                graphics.DrawString(
                                    title,
                                    font,
                                    solidWhite,
                                    new PointF(
                                        x + ((Math.Max(2, tileSize - titleWidth) / 2) - 1),
                                        y + imageSize + padding
                                    )
                                );
                            }
                        }
                        x += tileSize;
                    }
                    y += tileSize;
                }
            }

            using (var mosaicStream = new MemoryStream())
            {
                mosaic.Save(mosaicStream, ImageFormat.Png);
                var mosaicRaw = mosaicStream.ToArray();
                return new GetImageMosaicResponse
                {
                    Data = mosaicRaw,
                    MimeType = "image/png"
                };
            }
        }

        public async Task<GetImageMosaicResponse> HandleAsync(GetTradeImageMosaicRequest request)
        {
            var haveImageSources = request.HaveImageSources.ToList();
            var haveTileCount = haveImageSources.Count();

            var wantImageSources = request.WantImageSources.ToList();
            var wantTileCount = wantImageSources.Count();
            if (wantTileCount < 1)
            {
                return null;
            }

            var columns = Math.Max(1, request.Columns);
            var rows = (int)Math.Ceiling((float)Math.Max(haveTileCount, wantTileCount) / columns);
            var fontSize = 24;
            var textPadding = (int)Math.Ceiling(fontSize * 0.5);
            var tileSize = Math.Max(8, request.TileSize);

            // Hydrate the image sources (but only as many as we need to render)
            var maxHaveTiles = Math.Min(haveTileCount, (columns * rows));
            var maxWantTiles = Math.Min(wantTileCount, (columns * rows));
            haveImageSources = haveImageSources.Take(maxHaveTiles).ToList();
            wantImageSources = wantImageSources.Take(maxWantTiles).ToList();
            await HydrateImageData(haveImageSources);
            await HydrateImageData(wantImageSources);

            var mosaic = new Bitmap((columns + 1 + columns) * tileSize, (rows * tileSize) + fontSize + (textPadding * 2));
            using (var graphics = Graphics.FromImage(mosaic))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var sansSerif = new FontFamily(GenericFontFamilies.SansSerif);
                var gradientTop = Color.FromArgb(133, 133, 133);
                var gradientBottom = Color.FromArgb(99, 99, 99);
                var bluePen = new Pen(Color.FromArgb(66, 66, 66), 3);
                bluePen.LineJoin = LineJoin.Round;

                var x = 0;
                var y = 0;

                if (haveImageSources.Any())
                {
                    var path = new GraphicsPath();
                    path.AddString(
                        $"Have", sansSerif, (int)FontStyle.Bold, fontSize, new PointF(x + textPadding, y + textPadding), StringFormat.GenericTypographic
                    );
                    graphics.DrawPath(bluePen, path);
                    graphics.FillPath(
                        new LinearGradientBrush(new Rectangle(x + textPadding, y + textPadding, fontSize, fontSize), gradientTop, gradientBottom, LinearGradientMode.Vertical),
                        path
                    );

                    y += (fontSize + (textPadding * 2));
                    var haveImageQueue = new Queue<ImageSource>(haveImageSources);
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < columns; c++)
                        {
                            var imageSource = (haveImageQueue.Any() ? haveImageQueue.Dequeue() : null);
                            if (imageSource?.ImageData == null)
                            {
                                continue;
                            }

                            var image = Image.FromStream(new MemoryStream(imageSource.ImageData));
                            graphics.DrawImage(image, x + (c * tileSize), y + (r * tileSize), tileSize, tileSize);
                        }
                    }
                    x += ((columns * tileSize) + tileSize);
                    y = 0;
                }

                if (wantImageSources.Any())
                {
                    var path = new GraphicsPath();
                    path.AddString(
                        $"Want", sansSerif, (int)FontStyle.Bold, fontSize, new PointF(x + textPadding, y + textPadding), StringFormat.GenericTypographic
                    );
                    graphics.DrawPath(bluePen, path);
                    graphics.FillPath(
                        new LinearGradientBrush(new Rectangle(x + textPadding, y + textPadding, fontSize, fontSize), gradientTop, gradientBottom, LinearGradientMode.Vertical),
                        path
                    );

                    y += (fontSize + (textPadding * 2));
                    var wantImageQueue = new Queue<ImageSource>(wantImageSources);
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < columns; c++)
                        {
                            var imageSource = (wantImageQueue.Any() ? wantImageQueue.Dequeue() : null);
                            if (imageSource?.ImageData == null)
                            {
                                continue;
                            }

                            var image = Image.FromStream(new MemoryStream(imageSource.ImageData));
                            graphics.DrawImage(image, x + (c * tileSize), y + (r * tileSize), tileSize, tileSize);
                        }
                    }
                    x += ((columns * tileSize) + tileSize);
                    y = 0;
                }
            }

            using (var mosaicStream = new MemoryStream())
            {
                mosaic.Save(mosaicStream, ImageFormat.Png);
                var mosaicRaw = mosaicStream.ToArray();
                return new GetImageMosaicResponse
                {
                    Data = mosaicRaw,
                    MimeType = "image/png"
                };
            }
        }

        private async Task HydrateImageData(IEnumerable<ImageSource> imageSources)
        {
            // Check only images that are missing image data
            var missingImages = imageSources
                .Where(x => !String.IsNullOrEmpty(x.ImageUrl))
                .Where(x => x.ImageData == null)
                .ToList();
            if (!missingImages.Any())
            {
                return;
            }

            // Check the first-level cache (memory) for missing image data
            foreach (var imageSource in missingImages.ToList())
            {
                byte[] imageSourceData;
                if (Cache.TryGetValue(imageSource.ImageUrl, out imageSourceData))
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
            var missingImageData = _db.ImageData.AsNoTracking().Where(x => missingImageUrls.Contains(x.Source)).ToList();
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
                var fetchedImage = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                {
                    Url = imageSource.ImageUrl,
                    UseExisting = false, // we've already checked, it doesn't exist
                    ExpiresOn = DateTimeOffset.Now.AddDays(7) // cache for 7 days, then delete
                });
                if (fetchedImage?.Image?.Data != null)
                {
                    Cache.Set(imageSource.ImageUrl, fetchedImage.Image.Data);
                    imageSource.ImageData = fetchedImage.Image.Data;
                }
            }
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);
            var path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Top left arc  
            path.AddArc(arc, 180, 90);

            // Top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
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
