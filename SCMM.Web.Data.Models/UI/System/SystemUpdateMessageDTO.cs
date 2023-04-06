namespace SCMM.Web.Data.Models.UI.System;

public class SystemUpdateMessageDTO
{
    public DateTimeOffset Timestamp { get; set; }

    public string Description { get; set; }

    public Dictionary<string, string> Media { get; set; }
}
