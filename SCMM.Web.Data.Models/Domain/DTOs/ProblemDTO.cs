namespace SCMM.Web.Data.Models.Domain.DTOs
{
    public class ProblemDTO
    {
        public int Status { get; set; }

        public string Message { get; set; }

        public string Details { get; set; }

        public string TraceId { get; set; }
    }
}
