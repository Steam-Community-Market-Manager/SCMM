using SCMM.Steam.Data.Models;
using System;

namespace SCMM.Steam.Client.Exceptions
{
    public class SteamRequestException : Exception
    {
        public ISteamError Error { get; set; }

        public SteamRequestException() : base()
        {
        }

        public SteamRequestException(string message) : base(message)
        {
        }

        public SteamRequestException(string message, Exception innerException, ISteamError error = null) : base(message, innerException)
        {
            Error = error;
        }
    }
}
