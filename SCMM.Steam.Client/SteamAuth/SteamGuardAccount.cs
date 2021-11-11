using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SteamAuth
{
    public class SteamGuardAccount
    {
        [JsonPropertyName("shared_secret")]
        public string SharedSecret { get; set; }

        [JsonPropertyName("serial_number")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("revocation_code")]
        public string RevocationCode { get; set; }

        [JsonPropertyName("uri")]
        public string URI { get; set; }

        [JsonPropertyName("server_time")]
        public long ServerTime { get; set; }

        [JsonPropertyName("account_name")]
        public string AccountName { get; set; }

        [JsonPropertyName("token_gid")]
        public string TokenGID { get; set; }

        [JsonPropertyName("identity_secret")]
        public string IdentitySecret { get; set; }

        [JsonPropertyName("secret_1")]
        public string Secret1 { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("device_id")]
        public string DeviceID { get; set; }

        /// <summary>
        /// Set to true if the authenticator has actually been applied to the account.
        /// </summary>
        [JsonPropertyName("fully_enrolled")]
        public bool FullyEnrolled { get; set; }

        public SessionData Session { get; set; }

        private static readonly byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        public bool DeactivateAuthenticator(int scheme = 2)
        {
            var postData = new NameValueCollection
            {
                { "steamid", Session.SteamID.ToString() },
                { "steamguard_scheme", scheme.ToString() },
                { "revocation_code", RevocationCode },
                { "access_token", Session.OAuthToken }
            };

            try
            {
                var response = SteamWeb.MobileLoginRequest(APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/RemoveAuthenticator/v0001", "POST", postData);
                var removeResponse = JsonSerializer.Deserialize<RemoveAuthenticatorResponse>(response);

                if (removeResponse == null || removeResponse.Response == null || !removeResponse.Response.Success)
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GenerateSteamGuardCode()
        {
            return GenerateSteamGuardCodeForTime(TimeAligner.GetSteamTime());
        }

        public string GenerateSteamGuardCodeForTime(long time)
        {
            if (SharedSecret == null || SharedSecret.Length == 0)
            {
                return "";
            }

            var sharedSecretUnescaped = Regex.Unescape(SharedSecret);
            var sharedSecretArray = Convert.FromBase64String(sharedSecretUnescaped);
            var timeArray = new byte[8];

            time /= 30L;

            for (var i = 8; i > 0; i--)
            {
                timeArray[i - 1] = (byte)time;
                time >>= 8;
            }

            var hmacGenerator = new HMACSHA1
            {
                Key = sharedSecretArray
            };
            var hashedData = hmacGenerator.ComputeHash(timeArray);
            var codeArray = new byte[5];
            try
            {
                var b = (byte)(hashedData[19] & 0xF);
                var codePoint = (hashedData[b] & 0x7F) << 24 | (hashedData[b + 1] & 0xFF) << 16 | (hashedData[b + 2] & 0xFF) << 8 | (hashedData[b + 3] & 0xFF);

                for (var i = 0; i < 5; ++i)
                {
                    codeArray[i] = steamGuardCodeTranslations[codePoint % steamGuardCodeTranslations.Length];
                    codePoint /= steamGuardCodeTranslations.Length;
                }
            }
            catch (Exception)
            {
                return null; //Change later, catch-alls are bad!
            }
            return Encoding.UTF8.GetString(codeArray);
        }

        public Confirmation[] FetchConfirmations()
        {
            var url = GenerateConfirmationURL();

            var cookies = new CookieContainer();
            Session.AddCookies(cookies);

            var response = SteamWeb.Request(url, "GET", "", cookies);
            return FetchConfirmationInternal(response);
        }

        public async Task<Confirmation[]> FetchConfirmationsAsync()
        {
            var url = GenerateConfirmationURL();

            var cookies = new CookieContainer();
            Session.AddCookies(cookies);

            var response = await SteamWeb.RequestAsync(url, "GET", null, cookies);
            return FetchConfirmationInternal(response);
        }

        private Confirmation[] FetchConfirmationInternal(string response)
        {

            /*So you're going to see this abomination and you're going to be upset.
              It's understandable. But the thing is, regex for HTML -- while awful -- makes this way faster than parsing a DOM, plus we don't need another library.
              And because the data is always in the same place and same format... It's not as if we're trying to naturally understand HTML here. Just extract strings.
              I'm sorry. */

            var confRegex = new Regex("<div class=\"mobileconf_list_entry\" id=\"conf[0-9]+\" data-confid=\"(\\d+)\" data-key=\"(\\d+)\" data-type=\"(\\d+)\" data-creator=\"(\\d+)\"");

            if (response == null || !confRegex.IsMatch(response))
            {
                if (response == null || !response.Contains("<div>Nothing to confirm</div>"))
                {
                    throw new WGTokenInvalidException();
                }

                return new Confirmation[0];
            }

            var confirmations = confRegex.Matches(response);

            var ret = new List<Confirmation>();
            foreach (Match confirmation in confirmations)
            {
                if (confirmation.Groups.Count != 5)
                {
                    continue;
                }

                if (!ulong.TryParse(confirmation.Groups[1].Value, out var confID) ||
                    !ulong.TryParse(confirmation.Groups[2].Value, out var confKey) ||
                    !int.TryParse(confirmation.Groups[3].Value, out var confType) ||
                    !ulong.TryParse(confirmation.Groups[4].Value, out var confCreator))
                {
                    continue;
                }

                ret.Add(new Confirmation(confID, confKey, confType, confCreator));
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Deprecated. Simply returns conf.Creator.
        /// </summary>
        /// <param name="conf"></param>
        /// <returns>The Creator field of conf</returns>
        public long GetConfirmationTradeOfferID(Confirmation conf)
        {
            if (conf.ConfType != Confirmation.ConfirmationType.Trade)
            {
                throw new ArgumentException("conf must be a trade confirmation.");
            }

            return (long)conf.Creator;
        }

        public bool AcceptMultipleConfirmations(Confirmation[] confs)
        {
            return _sendMultiConfirmationAjax(confs, "allow");
        }

        public bool DenyMultipleConfirmations(Confirmation[] confs)
        {
            return _sendMultiConfirmationAjax(confs, "cancel");
        }

        public bool AcceptConfirmation(Confirmation conf)
        {
            return _sendConfirmationAjax(conf, "allow");
        }

        public bool DenyConfirmation(Confirmation conf)
        {
            return _sendConfirmationAjax(conf, "cancel");
        }

        /// <summary>
        /// Refreshes the Steam session. Necessary to perform confirmations if your session has expired or changed.
        /// </summary>
        /// <returns></returns>
        public bool RefreshSession()
        {
            var url = APIEndpoints.MOBILEAUTH_GETWGTOKEN;
            var postData = new NameValueCollection
            {
                { "access_token", Session.OAuthToken }
            };

            string response = null;
            try
            {
                response = SteamWeb.Request(url, "POST", postData);
            }
            catch (WebException)
            {
                return false;
            }

            if (response == null)
            {
                return false;
            }

            try
            {
                var refreshResponse = JsonSerializer.Deserialize<RefreshSessionDataResponse>(response);
                if (refreshResponse == null || refreshResponse.Response == null || string.IsNullOrEmpty(refreshResponse.Response.Token))
                {
                    return false;
                }

                var token = Session.SteamID + "%7C%7C" + refreshResponse.Response.Token;
                var tokenSecure = Session.SteamID + "%7C%7C" + refreshResponse.Response.TokenSecure;

                Session.SteamLogin = token;
                Session.SteamLoginSecure = tokenSecure;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Refreshes the Steam session. Necessary to perform confirmations if your session has expired or changed.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RefreshSessionAsync()
        {
            var url = APIEndpoints.MOBILEAUTH_GETWGTOKEN;
            var postData = new NameValueCollection
            {
                { "access_token", Session.OAuthToken }
            };

            string response = null;
            try
            {
                response = await SteamWeb.RequestAsync(url, "POST", postData);
            }
            catch (WebException)
            {
                return false;
            }

            if (response == null)
            {
                return false;
            }

            try
            {
                var refreshResponse = JsonSerializer.Deserialize<RefreshSessionDataResponse>(response);
                if (refreshResponse == null || refreshResponse.Response == null || string.IsNullOrEmpty(refreshResponse.Response.Token))
                {
                    return false;
                }

                var token = Session.SteamID + "%7C%7C" + refreshResponse.Response.Token;
                var tokenSecure = Session.SteamID + "%7C%7C" + refreshResponse.Response.TokenSecure;

                Session.SteamLogin = token;
                Session.SteamLoginSecure = tokenSecure;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private ConfirmationDetailsResponse _getConfirmationDetails(Confirmation conf)
        {
            var url = APIEndpoints.COMMUNITY_BASE + "/mobileconf/details/" + conf.ID + "?";
            var queryString = GenerateConfirmationQueryParams("details");
            url += queryString;

            var cookies = new CookieContainer();
            Session.AddCookies(cookies);
            var referer = GenerateConfirmationURL();

            var response = SteamWeb.Request(url, "GET", "", cookies, null);
            if (string.IsNullOrEmpty(response))
            {
                return null;
            }

            var confResponse = JsonSerializer.Deserialize<ConfirmationDetailsResponse>(response);
            if (confResponse == null)
            {
                return null;
            }

            return confResponse;
        }

        private bool _sendConfirmationAjax(Confirmation conf, string op)
        {
            var url = APIEndpoints.COMMUNITY_BASE + "/mobileconf/ajaxop";
            var queryString = "?op=" + op + "&";
            queryString += GenerateConfirmationQueryParams(op);
            queryString += "&cid=" + conf.ID + "&ck=" + conf.Key;
            url += queryString;

            var cookies = new CookieContainer();
            Session.AddCookies(cookies);
            var referer = GenerateConfirmationURL();

            var response = SteamWeb.Request(url, "GET", "", cookies, null);
            if (response == null)
            {
                return false;
            }

            var confResponse = JsonSerializer.Deserialize<SendConfirmationResponse>(response);
            return confResponse.Success;
        }

        private bool _sendMultiConfirmationAjax(Confirmation[] confs, string op)
        {
            var url = APIEndpoints.COMMUNITY_BASE + "/mobileconf/multiajaxop";

            var query = "op=" + op + "&" + GenerateConfirmationQueryParams(op);
            foreach (var conf in confs)
            {
                query += "&cid[]=" + conf.ID + "&ck[]=" + conf.Key;
            }

            var cookies = new CookieContainer();
            Session.AddCookies(cookies);
            var referer = GenerateConfirmationURL();

            var response = SteamWeb.Request(url, "POST", query, cookies, null);
            if (response == null)
            {
                return false;
            }

            var confResponse = JsonSerializer.Deserialize<SendConfirmationResponse>(response);
            return confResponse.Success;
        }

        public string GenerateConfirmationURL(string tag = "conf")
        {
            var endpoint = APIEndpoints.COMMUNITY_BASE + "/mobileconf/conf?";
            var queryString = GenerateConfirmationQueryParams(tag);
            return endpoint + queryString;
        }

        public string GenerateConfirmationQueryParams(string tag)
        {
            if (string.IsNullOrEmpty(DeviceID))
            {
                throw new ArgumentException("Device ID is not present");
            }

            var queryParams = GenerateConfirmationQueryParamsAsNVC(tag);

            return "p=" + queryParams["p"] + "&a=" + queryParams["a"] + "&k=" + queryParams["k"] + "&t=" + queryParams["t"] + "&m=android&tag=" + queryParams["tag"];
        }

        public NameValueCollection GenerateConfirmationQueryParamsAsNVC(string tag)
        {
            if (string.IsNullOrEmpty(DeviceID))
            {
                throw new ArgumentException("Device ID is not present");
            }

            var time = TimeAligner.GetSteamTime();

            var ret = new NameValueCollection
            {
                { "p", DeviceID },
                { "a", Session.SteamID.ToString() },
                { "k", _generateConfirmationHashForTime(time, tag) },
                { "t", time.ToString() },
                { "m", "android" },
                { "tag", tag }
            };

            return ret;
        }

        private string _generateConfirmationHashForTime(long time, string tag)
        {
            var decode = Convert.FromBase64String(IdentitySecret);
            var n2 = 8;
            if (tag != null)
            {
                if (tag.Length > 32)
                {
                    n2 = 8 + 32;
                }
                else
                {
                    n2 = 8 + tag.Length;
                }
            }
            var array = new byte[n2];
            var n3 = 8;
            while (true)
            {
                var n4 = n3 - 1;
                if (n3 <= 0)
                {
                    break;
                }
                array[n4] = (byte)time;
                time >>= 8;
                n3 = n4;
            }
            if (tag != null)
            {
                Array.Copy(Encoding.UTF8.GetBytes(tag), 0, array, 8, n2 - 8);
            }

            try
            {
                var hmacGenerator = new HMACSHA1
                {
                    Key = decode
                };
                var hashedData = hmacGenerator.ComputeHash(array);
                var encodedData = Convert.ToBase64String(hashedData, Base64FormattingOptions.None);
                var hash = WebUtility.UrlEncode(encodedData);
                return hash;
            }
            catch
            {
                return null;
            }
        }

        public class WGTokenInvalidException : Exception
        {
        }

        public class WGTokenExpiredException : Exception
        {
        }

        private class RefreshSessionDataResponse
        {
            [JsonPropertyName("response")]
            public RefreshSessionDataInternalResponse Response { get; set; }
            internal class RefreshSessionDataInternalResponse
            {
                [JsonPropertyName("token")]
                public string Token { get; set; }

                [JsonPropertyName("token_secure")]
                public string TokenSecure { get; set; }
            }
        }

        private class RemoveAuthenticatorResponse
        {
            [JsonPropertyName("response")]
            public RemoveAuthenticatorInternalResponse Response { get; set; }

            internal class RemoveAuthenticatorInternalResponse
            {
                [JsonPropertyName("success")]
                public bool Success { get; set; }
            }
        }

        private class SendConfirmationResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }

        private class ConfirmationDetailsResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("html")]
            public string HTML { get; set; }
        }
    }
}
