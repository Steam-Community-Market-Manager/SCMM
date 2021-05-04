using System;
using System.Xml.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Xml
{
    [Serializable, XmlRoot("profile")]
    public class SteamProfileXmlResponse
    {
        [XmlElement("steamID64")]
        public long SteamID64 { get; set; }

        [XmlElement("steamID")]
        public string SteamID { get; set; }

        [XmlElement("onlineState")]
        public string OnlineState { get; set; }

        [XmlElement("stateMessage")]
        public string StateMessage { get; set; }

        /// <summary>
        /// public, friendsonly, private
        /// </summary>
        [XmlElement("privacyState")]
        public string PrivacyState { get; set; }

        /// <summary>
        /// public=3, friendsonly=1, private=1
        /// </summary>
        [XmlElement("visibilityState")]
        public int VisibilityState { get; set; }

        [XmlElement("avatarIcon")]
        public string AvatarIcon { get; set; }

        [XmlElement("avatarMedium")]
        public string AvatarMedium { get; set; }

        [XmlElement("avatarFull")]
        public string AvatarFull { get; set; }

        [XmlElement("vacBanned")]
        public int VacBanned { get; set; }

        [XmlElement("tradeBanState")]
        public string TradeBanState { get; set; }

        [XmlElement("isLimitedAccount")]
        public int IsLimitedAccount { get; set; }

        [XmlElement("customURL")]
        public string CustomUrl { get; set; }

        [XmlElement("memberSince")]
        public string MemberSince { get; set; }

        [XmlElement("steamRating")]
        public string SteamRating { get; set; }

        [XmlElement("hoursPlayed2Wk")]
        public string HoursPlayed2Wk { get; set; }

        [XmlElement("headline")]
        public string Headline { get; set; }

        [XmlElement("location")]
        public string Location { get; set; }

        [XmlElement("realname")]
        public string RealName { get; set; }

        [XmlElement("summary")]
        public string Summary { get; set; }
    }
}