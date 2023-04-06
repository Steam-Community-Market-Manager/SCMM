using SCMM.Steam.Data.Models;

namespace SCMM.Steam.Client;

public abstract class SteamFormDataRequest : SteamRequest
{
    public abstract IDictionary<string, string> Data { get; }

    public static implicit operator FormUrlEncodedContent(SteamFormDataRequest x)
    {
        return new FormUrlEncodedContent(x.Data);
    }
}
