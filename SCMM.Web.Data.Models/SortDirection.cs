using System.ComponentModel;

namespace SCMM.Web.Data.Models
{
    public enum SortDirection
    {
        [Description("none")]
        None = 0,

        [Description("ascending")]
        Ascending,

        [Description("descending")]
        Descending
    }
}
