using System.Collections;

namespace SCMM.Shared.Data.Models
{
    public class PaginatedResult<T> : IEnumerable
    {
        /// <summary>
        /// The requested items
        /// </summary>
        public T[] Items { get; set; }

        /// <summary>
        /// Starting index of items included in this response
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Number of items included in this response
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Number of total available items
        /// </summary>
        public int Total { get; set; }

        public IEnumerator GetEnumerator()
        {
            return Items?.GetEnumerator();
        }
    }
}
