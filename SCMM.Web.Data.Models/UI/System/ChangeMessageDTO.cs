﻿using System;

namespace SCMM.Web.Data.Models.UI.System;

public class ChangeMessageDTO
{
    public DateTimeOffset Timestamp { get; set; }

    public string Description { get; set; }

    public IDictionary<string, string> Media { get; set; }
}