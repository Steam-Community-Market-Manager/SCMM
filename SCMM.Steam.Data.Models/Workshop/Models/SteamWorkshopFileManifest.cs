namespace SCMM.Steam.Data.Models.Workshop.Models
{
    public class SteamWorkshopFileManifest
    {
        public uint Version { get; set; }

        public string ItemType { get; set; }

        public ulong AuthorId { get; set; }

        public DateTimeOffset PublishDate { get; set; }

        public SteamWorkshopFileManifestGroup[] Groups { get; set; }

    }
}
