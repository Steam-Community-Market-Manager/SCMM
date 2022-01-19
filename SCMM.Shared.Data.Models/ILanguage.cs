namespace SCMM.Shared.Data.Models
{
    public interface ILanguage
    {
        public string Id { get; }

        public string Name { get; set; }

        public string CultureName { get; set; }
    }
}
