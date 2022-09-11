using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SCMM.Steam.API.Extensions
{
    public static class BitmapExtensions
    {
        public static decimal GetAlphaCuttoffRatio(this Image<Rgba32> image, decimal alphaCutoff = 1)
        {
            var alphaCutoffValue = (uint)Math.Round(alphaCutoff * 255, 0);
            var totalPixelCount = image.Width * image.Height;
            var transparentPixelCount = 0L;
            for (var x = 0; x < image.Width; x += 4)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var pixel = image[x, y];
                    if (pixel.A < alphaCutoffValue)
                    {
                        transparentPixelCount++;
                    }
                }
            }

            return transparentPixelCount > 0 && totalPixelCount > 0
                ? transparentPixelCount / (decimal)totalPixelCount
                : transparentPixelCount > 0 ? 1 : 0;
        }

        public static decimal GetEmissionRatio(this Image<Rgba32> image)
        {
            var totalPixelCount = image.Width * image.Height;
            var nonBlackPixelCount = 0L;
            for (var x = 0; x < image.Width; x += 4)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var pixel = image[x, y];
                    if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
                    {
                        nonBlackPixelCount++;
                    }
                }
            }

            return nonBlackPixelCount > 0 && totalPixelCount > 0
                ? nonBlackPixelCount / (decimal)totalPixelCount
                : nonBlackPixelCount > 0 ? 1 : 0;
        }
    }
}
