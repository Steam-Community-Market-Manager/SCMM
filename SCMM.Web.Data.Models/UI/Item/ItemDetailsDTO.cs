using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.Domain.Currencies;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDetailsDTO : IItemDescription, IPurchasable, ISubscribable, ISearchable
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public string ItemType { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public PriceType? BuyNowFrom { get; set; }

        public CurrencyDTO BuyNowCurrency { get; set; }

        public long? BuyNowPrice { get; set; }

        public string BuyNowUrl { get; set; }

        public long? Subscriptions { get; set;  }

        [JsonIgnore]
        public object[] SearchData => new object[] { Id, Name, ItemType };
    }
}
