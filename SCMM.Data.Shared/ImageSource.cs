namespace SCMM.Data.Shared
{
    public enum ImageSymbol
    {
        None = 0,
        ChevronUp,
        ChevronDown,
        Cross
    }

    public class ImageSource
    {
        public string ImageUrl { get; set; }

        public byte[] ImageData { get; set; }

        public string Title { get; set; }

        public int Badge { get; set; }

        public ImageSymbol Symbol { get; set; }
    }
}
