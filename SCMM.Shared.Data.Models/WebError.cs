﻿namespace SCMM.Shared.Data.Models
{
    public class WebError
    {
        public int Status { get; set; }

        public string Message { get; set; }

        public string Details { get; set; }

        public string TraceId { get; set; }
    }
}
