using System.Text;

namespace SCMM.Shared.Web.Formatters
{
    public class XlsxFormatterOptions
    {
        public Encoding Encoding { get; set; } = Encoding.Default;

        public bool UseJsonAttributes { get; set; } = true;
    }
}