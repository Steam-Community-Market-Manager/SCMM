using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace SCMM.Web.Client.Components.DataTable
{
    public class BaseDataTableRow<TItem> : BaseMatDomComponent
    {
        public BaseDataTableRow()
        {
            ClassMapper
                .Add("mdc-data-table__row")
                .If("mdc-table-row-hover", () => AllowSelection)
                .If("mdc-table-row-selected", () => Selected);
        }

        [CascadingParameter()]
        public BaseDataTable<TItem> DataTable { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool Selected { get; set; }

        [Parameter]
        public bool AllowSelection { get; set; }

        [Parameter]
        public TItem RowItem { get; set; }

        [Parameter]
        public EventCallback<bool> SelectedChanged { get; set; }

        public async Task ToggleSelectedAsync()
        {
            this.Selected = !this.Selected;
            await SelectedChanged.InvokeAsync(this.Selected);
            await this.DataTable.ToggleSelectedRowAsync(this);
            this.StateHasChanged();
        }

        protected async void OnClickHandler(MouseEventArgs e)
        {
            if (this.AllowSelection)
            {
                await this.ToggleSelectedAsync();
            }
        }
    }
}
