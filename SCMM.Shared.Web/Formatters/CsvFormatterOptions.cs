using System.Text;

namespace SCMM.Shared.Web.Formatters
{
    public class CsvFormatterOptions
    {
        public Encoding Encoding { get; set; } = Encoding.Default;

        public bool UseJsonAttributes { get; set; } = true;

        public bool UseSingleLineHeader { get; set; } = true;

        public bool IncludeExcelDelimiterHeader { get; set; } = false;

        public bool ReplaceLineBreakCharacters { get; set; } = true;

        public string Delimiter { get; set; } = ",";
    }
}