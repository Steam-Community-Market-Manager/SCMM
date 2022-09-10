using SCMM.Steam.Data.Models;
using System.Net;

namespace SCMM.Steam.Client.Exceptions
{
    public class SteamRequestException : Exception
    {
        public SteamRequestException(HttpStatusCode? statusCode = null) : base()
        {
            StatusCode = statusCode;
        }

        public SteamRequestException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public SteamRequestException(string message, HttpStatusCode? statusCode = null, ISteamError error = null) : base(message)
        {
            StatusCode = statusCode;
            Error = error;
        }

        public SteamRequestException(string message, HttpStatusCode? statusCode = null, Exception innerException = null) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public ISteamError Error { get; set; }

        public HttpStatusCode? StatusCode { get; set; }

        public bool IsRateLimited => (
            StatusCode == HttpStatusCode.TooManyRequests
        );

        public bool IsAuthenticiationRequired => (
            StatusCode == HttpStatusCode.BadRequest ||
            StatusCode == HttpStatusCode.Unauthorized ||
            StatusCode == HttpStatusCode.Forbidden
        );
    }
}
