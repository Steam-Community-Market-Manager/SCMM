namespace SCMM.Web.Data.Models.UI.Diff;

public class TextDiffDTO
{
    public TextDiffPieceDTO[] Lines { get; set; }

    public bool HasDifferences => Lines.Any(x => x.Type != TextDiffChangeType.Unchanged.ToString());
}
