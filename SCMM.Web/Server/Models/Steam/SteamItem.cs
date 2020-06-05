using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Web.Server.Models.Steam
{
    public class SteamItem : Entity
    {
        public SteamItem()
        {
            BuyOrders = new Collection<SteamItemOrder>();
            SellOrders = new Collection<SteamItemOrder>();
        }

        // TODO: Make this property required
        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public Guid DescriptionId { get; set; }

        public SteamItemDescription Description { get; set; }

        [ForeignKey("BuyOrderItemId")]
        public ICollection<SteamItemOrder> BuyOrders { get; set; }

        [ForeignKey("SellOrderItemId")]
        public ICollection<SteamItemOrder> SellOrders { get; set; }

        public DateTimeOffset LastChecked { get; set; }
    }
}
