using System.Xml.Serialization;

namespace SCMM.Steam.Data.Models.Community.Responses.Xml
{
    [Serializable, XmlRoot("response")]
    public class SteamErrorXmlResponse : ISteamError
    {
        [XmlElement("error")]
        public string Error { get; set; }

        [XmlIgnore]
        public string Message => Error;
    }
}