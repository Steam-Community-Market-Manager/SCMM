using Microsoft.JSInterop;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI;

namespace SCMM.Web.Client.Shared.Navigation;

public class ExternalNavigationManager
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AppState _state;

    public ExternalNavigationManager(IJSRuntime jsRuntime, AppState state)
    {
        _jsRuntime = jsRuntime;
        _state = state;
    }

    public void NavigateTo(string uri)
    {
        _jsRuntime.InvokeVoidAsync("WindowInterop.open", uri);
    }

    public void NavigateToNewTab(string uri)
    {
        _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", uri);
    }

    public void NavigateToItem(IItemDescription item)
    {
        switch (_state?.Profile?.ItemInfoWebsite ?? ItemInfoWebsiteType.External)
        {
            case ItemInfoWebsiteType.Internal:
                _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", $"/item/{item.Name}");
                break;

            case ItemInfoWebsiteType.External:
                var purchasableItem = (item as ICanBePurchased);
                if (purchasableItem != null && !String.IsNullOrEmpty(purchasableItem.BuyNowUrl))
                {
                    _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", purchasableItem.BuyNowUrl);
                }
                else // TODO: if (item.IsMarketable)
                {
                    _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", new SteamMarketListingPageRequest()
                    {
                        AppId = item.AppId.ToString(),
                        MarketHashName = item.Name
                    }.ToString());
                }
                break;
        }
    }
}
