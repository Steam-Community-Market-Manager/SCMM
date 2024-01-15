namespace SCMM.Web.Data.Models.UI.Diff;

public class TextDiffPieceDTO
{
    public string Type { get; set; }

    public int? Position { get; set; }

    public string Text { get; set; }

    public TextDiffPieceDTO[] SubPieces { get; set; }

}
