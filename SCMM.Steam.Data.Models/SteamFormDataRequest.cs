namespace SCMM.Steam.Data.Models;

public abstract class SteamFormDataRequest : SteamRequest
{
    public abstract IDictionary<string, string> Data { get; }

    public static implicit operator FormUrlEncodedContent(SteamFormDataRequest x)
    {
        return (x?.Data == null) ? null : new FormUrlEncodedContent(x.Data);
    }
}
