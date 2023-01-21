namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusAlertDTO
{
    public SystemStatusAlertSeverity Severity { get; set; }

    public string Message { get; set; }

    public bool IsPersistent { get; set; }

    public bool IsVisible { get; set; }
}
