using Microsoft.Extensions.Caching.Memory;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Blob;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services
{
    public class ImageService
    {
        private static IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamCommunityClient _client;

        public ImageService(SteamCommunityClient client)
        {
            _client = client;
        }

        public async Task<byte[]> GetImageCached(string url)
        {
            byte[] image;
            if (Cache.TryGetValue(url, out image))
            {
                return image;
            }

            image = await _client.GetImage(new SteamBlobRequest(url));
            if (image == null)
            {
                return null;
            }

            Cache.Set(url, image);
            return image;
        }

        public async Task<byte[]> GetImageMosaic(IEnumerable<ImageSource> imageSources, int tileSize = 128, int columns = 5, int rows = 5)
        {
            var tileCount = imageSources.Count();
            if (tileCount < 1)
            {
                return null;
            }

            columns = Math.Max(1, columns);
            rows = Math.Max(1, rows);
            tileSize = Math.Max(8, tileSize);

            // If there are rows than we have images for, reduces the row count to the minimum required to render the images
            var minimumRowsToRenderTiles = (int)Math.Ceiling((float)tileCount / columns);
            rows = Math.Min(minimumRowsToRenderTiles, rows);

            var mosaic = new Bitmap(columns * tileSize, rows * tileSize);
            using (var graphics = Graphics.FromImage(mosaic))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var badgeSize = (int) Math.Ceiling(tileSize * 0.25f);
                var badgePadding = (int) Math.Ceiling(tileSize * 0.0625f);
                var sansSerif = new FontFamily(GenericFontFamilies.SansSerif);
                var solidBlack = new SolidBrush(Color.FromArgb(255, 0, 0, 0));
                var solidBlue = new SolidBrush(Color.FromArgb(255, 144, 202, 249));
                var imageQueue = new Queue<ImageSource>(imageSources);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        var imageSource = (imageQueue.Any() ? imageQueue.Dequeue() : null);
                        if (imageSource == null)
                        {
                            continue;
                        }

                        var imageRaw = await GetImageCached(imageSource.Url);
                        if (imageRaw == null)
                        {
                            continue;
                        }

                        var image = Image.FromStream(new MemoryStream(imageRaw));
                        graphics.DrawImage(image, c * tileSize, r * tileSize, tileSize, tileSize);

                        var count = Math.Min(99, imageSource.BadgeCount);
                        if (count > 1)
                        {
                            var fontSize = 24;
                            var fontOffset = 2;
                            if (count >= 10)
                            {
                                fontSize = 20;
                                fontOffset = 5;
                            }
                            graphics.FillEllipse(
                                solidBlue, 
                                new Rectangle(
                                    (c * tileSize) + tileSize - badgeSize, 
                                    (r * tileSize), 
                                    badgeSize, 
                                    badgeSize
                                )
                            );
                            graphics.DrawString(
                                $"{count}",
                                new Font(sansSerif, fontSize, FontStyle.Regular, GraphicsUnit.Pixel), 
                                solidBlack, 
                                new PointF(
                                    (c * tileSize) + tileSize - (badgeSize - badgePadding + fontOffset), 
                                    ((r * tileSize) + (badgePadding / 4) + (fontOffset / 4))
                                )
                            );
                        }
                    }
                }
            }

            using (var mosaicStream = new MemoryStream())
            {
                mosaic.Save(mosaicStream, ImageFormat.Png);
                var mosaicRaw = mosaicStream.ToArray();
                return mosaicRaw;
            }
        }

        public async Task<byte[]> GetTradeImageMosaic(IEnumerable<ImageSource> haveImageSources, IEnumerable<ImageSource> wantImageSources, int fontSize = 48, int tileSize = 128)
        {
            var haveTileCount = haveImageSources.Count();
            var wantTileCount = wantImageSources.Count();
            if (wantTileCount < 1)
            {
                return null;
            }

            var columns = 5;
            var rows = 5;
            fontSize = Math.Max(24, fontSize);
            var textPadding = (int) Math.Ceiling(fontSize * 0.5);
            tileSize = Math.Max(8, tileSize);

            // If there are rows than we have images for, reduces the row count to the minimum required to render the images
            var minimumRowsToRenderTiles = (int)Math.Ceiling((float)Math.Max(haveTileCount, wantTileCount) / columns);
            rows = Math.Min(minimumRowsToRenderTiles, rows);

            var mosaic = new Bitmap((columns + 1 + columns) * tileSize, (rows * tileSize) + fontSize + (textPadding * 2));
            using (var graphics = Graphics.FromImage(mosaic))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var sansSerif = new FontFamily(GenericFontFamilies.SansSerif);
                var gradientTop = Color.FromArgb(66, 66, 66);
                var gradientBottom = Color.FromArgb(33, 33, 33);
                var bluePen = new Pen(Color.FromArgb(33, 33, 33), 3);
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
                            if (imageSource == null)
                            {
                                continue;
                            }

                            var imageRaw = await GetImageCached(imageSource.Url);
                            if (imageRaw == null)
                            {
                                continue;
                            }

                            var image = Image.FromStream(new MemoryStream(imageRaw));
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
                            if (imageSource == null)
                            {
                                continue;
                            }

                            var imageRaw = await GetImageCached(imageSource.Url);
                            if (imageRaw == null)
                            {
                                continue;
                            }

                            var image = Image.FromStream(new MemoryStream(imageRaw));
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
                return mosaicRaw;
            }
        }
    }
}
