using SCMM.Steam.Data.Models;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemStoreInstanceDTO
    {
        public string Id { get; set; }

        public DateTimeOffset? Date { get; set; }

        public string Name { get; set; }

        public long? Price { get; set; }

        public override string ToString()
        {
            if (Date != null)
            {
                return Date.Value.UtcDateTime.AddMinutes(1).ToString(Constants.SCMMStoreIdDateFormat);
            }
            if (!String.IsNullOrEmpty(Name))
            {
                return Name.ToLower();
            }

            return Id.ToString();
        }
    }
}
