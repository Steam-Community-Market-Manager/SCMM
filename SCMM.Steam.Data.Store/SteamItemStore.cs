using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace SCMM.Steam.Data.Store
{
    public class SteamItemStore : Entity
    {
        public SteamItemStore()
        {
            Items = new Collection<SteamStoreItemItemStore>();
            Media = new PersistableStringCollection();
        }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public string Name { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public ICollection<SteamStoreItemItemStore> Items { get; set; }

        public Guid? ItemsThumbnailId { get; set; }

        public ImageData ItemsThumbnail { get; set; }

        public PersistableStringCollection Media { get; set; }

        public string GetFullName()
        {
            var culture = CultureInfo.InvariantCulture;
            var storeDate = Start.UtcDateTime;
            var storeDateWeek = culture.Calendar.GetWeekOfYear(storeDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
            var storeDateYear = (storeDate.Year < DateTime.UtcNow.Year) ? storeDate.Year : 0;
            
            var name = new StringBuilder();
            if (storeDate.Year < DateTime.UtcNow.Year)
            {
                name.Append($"{storeDateYear} ");
            }
            name.Append($"Week {storeDateWeek}");
            if (!String.IsNullOrEmpty(Name))
            {
                name.Append($" \"{Name}\"");
            }

            return name.ToString();
        }
    }
}
