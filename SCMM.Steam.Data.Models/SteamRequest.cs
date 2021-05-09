using System;

namespace SCMM.Steam.Data.Models
{
    public abstract class SteamRequest
    {
        public abstract Uri Uri { get; }

        public static implicit operator string(SteamRequest x) => x.Uri.AbsoluteUri;
        public static implicit operator Uri(SteamRequest x) => x.Uri;
    }
}
