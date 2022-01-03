namespace SCMM.Shared.Data.Models.Extensions
{
    public static class MimeTypeExtensions
    {
        public static string GetFileExtension(this string mimeType)
        {
            if (mimeType?.StartsWith("text/") == true ||
                mimeType?.StartsWith("image/") == true ||
                mimeType?.StartsWith("video/") == true ||
                mimeType?.StartsWith("audio/") == true)
            {
                return mimeType.Split("/").LastOrDefault();
            }

            return "raw";
        }
    }
}
