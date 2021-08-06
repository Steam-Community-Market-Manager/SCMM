using System.Drawing;
using System.Drawing.Imaging;

namespace SCMM.Steam.API.Extensions
{
    public static class BitmapExtensions
    {
        public static decimal GetTransparencyRatio(this Image image, uint alphaCutoff = 255)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return GetTransparencyRatio(bitmap, alphaCutoff);
            }
            using (bitmap = new Bitmap(image))
            {
                return GetTransparencyRatio(bitmap, alphaCutoff);
            }
        }

        public static decimal GetTransparencyRatio(this Bitmap bitmap, uint alphaCutoff = 255)
        {
            var bitmapBounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(bitmapBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var totalPixelCount = (bitmap.Width * bitmap.Height);
            var transparentPixelCount = 0L;
            unsafe
            {
                var p = (byte*)bitmapData.Scan0;
                for (var x = 0; x < bitmap.Width; x += 4)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (p[x + y * bitmapData.Stride + 3] < alphaCutoff)
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
                        if (p[x + y * bitmapData.Stride + 0] != 0 || p[x + y * bitmapData.Stride + 1] != 0 || p[x + y * bitmapData.Stride + 2] != 0)
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
