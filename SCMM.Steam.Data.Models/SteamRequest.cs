namespace SCMM.Steam.Data.Models
{
    public abstract class SteamRequest
    {
        public abstract Uri Uri { get; }

        public override string ToString()
        {
            return Uri?.AbsoluteUri ?? base.ToString();
        }

        public static implicit operator string(SteamRequest x)
        {
            return x?.Uri?.AbsoluteUri;
        }

        public static implicit operator Uri(SteamRequest x)
        {
            return x?.Uri;
        }
    }
}
