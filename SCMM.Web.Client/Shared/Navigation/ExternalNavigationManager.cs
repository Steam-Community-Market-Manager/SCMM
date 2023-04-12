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

    public Task NavigateToAsync(string uri)
    {
        return _jsRuntime.InvokeVoidAsync("WindowInterop.open", uri).AsTask();
    }

    public Task NavigateToNewTabAsync(string uri)
    {
        return _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", uri).AsTask();
    }

    public Task NavigateToItemAsync(IItemDescription item)
    {
        switch (_state?.Profile?.ItemInfoWebsite ?? ItemInfoWebsiteType.External)
        {
            case ItemInfoWebsiteType.Internal:
                if (item.Id > 0)
                {
                    return _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", $"/item/{item.Id}").AsTask();
                }
                else
                {
                    return _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", $"/item/{item.Name}").AsTask();
                }

            case ItemInfoWebsiteType.External:
                var interactableItem = (item as ICanBeInteractedWith);
                var purchasableItem = (item as ICanBePurchased);
                if (purchasableItem != null && !String.IsNullOrEmpty(purchasableItem.BuyNowUrl))
                {
                    return _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", purchasableItem.BuyNowUrl).AsTask();
                }
                else if (interactableItem != null && interactableItem.Actions.Any(x => !String.IsNullOrEmpty(x.Url)))
                {
                    return _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", interactableItem.Actions.FirstOrDefault(x => !String.IsNullOrEmpty(x.Url)).Url).AsTask();
                }
                else // TODO: if (item.IsMarketable)
                {
                    return _jsRuntime.InvokeVoidAsync("WindowInterop.openInNewTab", new SteamMarketListingPageRequest()
                    {
                        AppId = item.AppId.ToString(),
                        MarketHashName = item.Name
                    }.ToString()).AsTask();
                }
        }

        return Task.CompletedTask;
    }
}
