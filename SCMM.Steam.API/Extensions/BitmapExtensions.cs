using System.Drawing.Imaging;
using System.Drawing;

namespace SCMM.Steam.API.Extensions
{
    public static class BitmapExtensions
    {
        public static bool HasTransparency(this Image image, uint alphaCutoff = 255, decimal transparencyRatio = 0.01m)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return HasTransparency(bitmap, alphaCutoff, transparencyRatio);
            }
            using (bitmap = new Bitmap(image))
            {
                return HasTransparency(bitmap, alphaCutoff, transparencyRatio);
            }
        }

        public static bool HasTransparency(this Bitmap bitmap, uint alphaCutoff = 255, decimal transparencyRatio = 0.01m)
        {
            var bitmapBounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(bitmapBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var opaquePixelCount = 0l;
            var transparentPixelCount = 0l;
            unsafe
            {
                var p = (byte*)bitmapData.Scan0;
                for (var x = 0; x < bitmap.Width; x += 4)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        var alpha = p[x + y * bitmapData.Stride + 3];
                        if (p[x + y * bitmapData.Stride + 3] < alphaCutoff)
                        {
                            transparentPixelCount++;
                        }
                        else
                        {
                            opaquePixelCount++;
                        }
                    }
                }
                bitmap.UnlockBits(bitmapData);
            }

            return (opaquePixelCount > 0 && transparentPixelCount > 0) 
                ? (((decimal)transparentPixelCount / (decimal)opaquePixelCount) >= transparencyRatio)
                : (transparentPixelCount > 0);
        }
    }
}
