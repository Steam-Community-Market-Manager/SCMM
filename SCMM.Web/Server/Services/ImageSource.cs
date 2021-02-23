using System;

namespace SCMM.Web.Server.Services
{
    public class ImageSource
    {
        public string ImageUrl { get; set; }

        public byte[] ImageData { get; set; }

        public string Title { get; set; }

        public int Badge { get; set; }

        public bool ChevronUp { get; set; }

        public bool ChevronDown { get; set; }
    }
}
