namespace SCMM.Web.Data.Models.UI.System;
public class SystemStatusAppItemDefinitionArchive
{
    public string Digest { get; set; }

    public int Size { get; set; }

    public DateTimeOffset PublishedOn { get; set; }

    public bool IsImported { get; set; }
}
