using Newtonsoft.Json;
using SCMM.Web.Data.Models.Domain.Currencies;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemCollectionDTO : ISearchable
    {
        public string Name { get; set; }

        public string AuthorName { get; set; }

        public string AuthorAvatarUrl { get; set; }

        public CurrencyDTO BuyNowCurrency { get; set; }

        public long? BuyNowPrice { get; set; }

        public IList<ItemDetailsDTO> Items { get; set; }

        [JsonIgnore]
        public object[] SearchData => new object[] { AuthorName, Name };
    }
}
