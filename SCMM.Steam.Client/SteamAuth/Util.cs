namespace SteamAuth
{
    public class Util
    {
        public static long GetSystemUnixTime()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            var hexLen = hex.Length;
            var ret = new byte[hexLen / 2];
            for (var i = 0; i < hexLen; i += 2)
            {
                ret[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return ret;
        }
    }
}
