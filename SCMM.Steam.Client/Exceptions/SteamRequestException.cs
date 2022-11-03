using SCMM.Steam.Data.Models;
using System.Net;

namespace SCMM.Steam.Client.Exceptions
{
    public class SteamRequestException : Exception
    {
        public SteamRequestException(string message) : base(message)
        {
            StatusCode = null;
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

        public bool IsTemporaryError => (
            StatusCode == HttpStatusCode.RequestTimeout ||
            StatusCode == HttpStatusCode.GatewayTimeout ||
            StatusCode == HttpStatusCode.BadGateway
        );

        public bool IsRateLimited => (
            StatusCode == HttpStatusCode.TooManyRequests
        );

        public bool IsProxyAuthenticationRequired => (
            StatusCode == HttpStatusCode.ProxyAuthenticationRequired
        );

        public bool IsSteamAuthenticationRequired => (
            StatusCode == HttpStatusCode.BadRequest ||
            StatusCode == HttpStatusCode.Unauthorized ||
            StatusCode == HttpStatusCode.Forbidden
        );

        public bool IsNotModified => (
            StatusCode == HttpStatusCode.NotModified
        );
    }
}
