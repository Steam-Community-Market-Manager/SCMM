using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Data.Models
{
    public enum RuntimeType
    {
        [Display(Name = "Server")]
        Server = 0,

        [Display(Name = "Client")]
        WebAssembly
    }
}
