namespace SCMM.Shared.Data.Models
{
    public interface IPaginated
    {
        public object[] Items { get; }

        public int Start { get; }

        public int Count { get; }

        public int Total { get; }
    }
}
