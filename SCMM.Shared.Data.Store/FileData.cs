using System;

namespace SCMM.Shared.Data.Store
{
    public class FileData : Entity
    {
        /// <summary>
        /// URL describing where this file originally came from
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The file name (if any)
        /// </summary>
        public string Name { get; set; }

        public string MimeType { get; set; }

        public byte[] Data { get; set; }

        /// <summary>
        /// If null, the file never expires
        /// </summary>
        public DateTimeOffset? ExpiresOn { get; set; }
    }
}
