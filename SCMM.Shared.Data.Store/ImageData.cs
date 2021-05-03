using System;

namespace SCMM.Shared.Data.Store
{
    public class ImageData : Entity
    {
        public string Source { get; set; }

        public string MimeType { get; set; }

        public byte[] Data { get; set; }

        public DateTimeOffset? ExpiresOn { get; set; }
    }
}
