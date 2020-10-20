using System;

namespace SCMM.Steam.Shared
{
    public abstract class SteamRequest
    {
        public abstract Uri Uri { get; }

        public static implicit operator string(SteamRequest x) => x.Uri.AbsoluteUri.ToString();
    }
}
