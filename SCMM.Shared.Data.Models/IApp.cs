namespace SCMM.Shared.Data.Models
{
    public interface IApp
    {
        public ulong Id { get; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string BackgroundColor { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }
    }
}
