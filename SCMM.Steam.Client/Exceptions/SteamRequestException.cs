using SCMM.Steam.Data.Models;
using System;
using System.Net;

namespace SCMM.Steam.Client.Exceptions
{
    public class SteamRequestException : Exception
    {
        public ISteamError Error { get; set; }

        public HttpStatusCode? StatusCode { get; set; }

        public SteamRequestException(HttpStatusCode? statusCode = null) : base()
        {
            StatusCode = statusCode;
        }

        public SteamRequestException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public SteamRequestException(string message, HttpStatusCode? statusCode = null, Exception innerException = null, ISteamError error = null) : base(message, innerException)
        {
            StatusCode = statusCode;
            Error = error;
        }
    }
}
