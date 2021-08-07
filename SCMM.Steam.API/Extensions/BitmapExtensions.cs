using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SCMM.Steam.API.Extensions
{
    public static class BitmapExtensions
    {
        public static decimal GetAlphaCuttoffRatio(this Image image, decimal alphaCutoff = 1)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return GetAlphaCuttoffRatio(bitmap, alphaCutoff);
            }
            using (bitmap = new Bitmap(image))
            {
                return GetAlphaCuttoffRatio(bitmap, alphaCutoff);
            }
        }

        public static decimal GetAlphaCuttoffRatio(this Bitmap bitmap, decimal alphaCutoff = 1)
        {
            var bitmapBounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(bitmapBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var alphaCutoffValue = (uint)Math.Round(alphaCutoff * 255, 0);
            var totalPixelCount = (bitmap.Width * bitmap.Height);
            var transparentPixelCount = 0L;
            unsafe
            {
                var p = (byte*)bitmapData.Scan0;
                for (var x = 0; x < bitmap.Width; x += 4)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (p[(x * 4) + (y * bitmapData.Stride) + 3] < alphaCutoffValue) // A
                        {
                            transparentPixelCount++;
                        }
                    }
                }
                bitmap.UnlockBits(bitmapData);
            }

            return (transparentPixelCount > 0 && totalPixelCount > 0)
                ? ((decimal)transparentPixelCount / (decimal)totalPixelCount)
                : (transparentPixelCount > 0 ? 1 : 0);
        }

        public static decimal GetEmissionRatio(this Image image)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return GetEmissionRatio(bitmap);
            }
            using (bitmap = new Bitmap(image))
            {
                return GetEmissionRatio(bitmap);
            }
        }

        public static decimal GetEmissionRatio(this Bitmap bitmap)
        {
            var bitmapBounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(bitmapBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var totalPixelCount = (bitmap.Width * bitmap.Height);
            var nonBlackPixelCount = 0L;
            unsafe
            {
                var p = (byte*)bitmapData.Scan0;
                for (var x = 0; x < bitmap.Width; x += 4)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (p[(x * 4) + (y * bitmapData.Stride) + 0] != 0 || // B
                            p[(x * 4) + (y * bitmapData.Stride) + 1] != 0 || // G
                            p[(x * 4) + (y * bitmapData.Stride) + 2] != 0) // R
                        {
                            nonBlackPixelCount++;
                        }
                    }
                }
                bitmap.UnlockBits(bitmapData);
            }

            return (nonBlackPixelCount > 0 && totalPixelCount > 0)
                ? ((decimal)nonBlackPixelCount / (decimal)totalPixelCount)
                : (nonBlackPixelCount > 0 ? 1 : 0);
        }
    }
}
