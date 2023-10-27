using System.Text;

namespace SCMM.Shared.Web.Server.Formatters
{
    public class CsvFormatterOptions
    {
        public Encoding Encoding { get; set; } = Encoding.Default;

        public string Delimiter { get; set; } = ",";

        public string ListDelimiter { get; set; } = ";";

        public bool IncludeExcelDelimiterHeader { get; set; } = false;

        public bool IncludeColumnNameHeader { get; set; } = true;

        public bool ReplaceLineBreakCharacters { get; set; } = true;

    }
}