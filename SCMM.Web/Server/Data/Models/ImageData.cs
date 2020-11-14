namespace SCMM.Web.Server.Data.Models.Steam
{
    public class ImageData : Entity
    {
        public string MineType { get; set; }

        public byte[] Value { get; set; }

        public byte[] ValueLarge { get; set; }
    }
}
