using System;

namespace SCMM.Web.Server.Models.Steam
{
    public class SteamItemOrder : Entity
    {
        public int Price { get; set; }

        public int Quantity { get; set; }
    }
}
