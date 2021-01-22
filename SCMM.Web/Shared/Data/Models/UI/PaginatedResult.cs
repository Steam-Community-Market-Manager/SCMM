namespace SCMM.Web.Shared.Data.Models.UI
{
    public class PaginatedResult<T>
    {
        public T[] Items { get; set; }
        
        public int Start { get; set; }

        public int Count { get; set; }

        public int Total { get; set; }
    }
}
