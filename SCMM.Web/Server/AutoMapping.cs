using AutoMapper;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Data.Models.UI.MarketStatistics;
using SCMM.Web.Shared.Data.Models.UI.ProfileInventory;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using SCMM.Web.Shared.Domain.DTOs.Languages;
using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using SCMM.Web.Shared.Domain.DTOs.StoreItems;

namespace SCMM.Web.Server
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<SteamLanguage, LanguageDTO>();
            CreateMap<SteamLanguage, LanguageListDTO>();
            CreateMap<SteamLanguage, LanguageDetailedDTO>();

            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamCurrency, CurrencyListDTO>();
            CreateMap<SteamCurrency, CurrencyDetailedDTO>();

            CreateMap<SteamProfile, ProfileDTO>();
            CreateMap<SteamProfile, ProfileDetailedDTO>();
            CreateMap<SteamProfile, ProfileSummaryDTO>();

            CreateMap<SteamAssetDescription, ProfileInventoryItemSummaryDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Flags, o => o.Ignore());

            CreateMap<SteamProfileInventoryItem, InventoryInvestmentItemDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyPrice, p => p.Currency))
                .ForMember(x => x.BuyPriceLocal, o => o.MapFrom(p => p.Currency != null && p.BuyPrice != null ? p.Currency.ToPriceString(p.BuyPrice.Value, true) : null))
                .ForMember(x => x.Last1hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.Last1hrValue : null, p => p.Description.MarketItem.Currency))
                .ForMember(x => x.ResellPrice, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.ResellPrice : null, p => p.Description.MarketItem.Currency))
                .ForMember(x => x.ResellTax, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.ResellTax : null, p => p.Description.MarketItem.Currency))
                .ForMember(x => x.ResellProfit, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.ResellProfit : null, p => p.Description.MarketItem.Currency));

            CreateMap<SteamProfileMarketItem, ProfileInventoryItemWishDTO>()
                .ForMember(x => x.SteamId, o => o.MapFrom(p => p.Description.SteamId))
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Supply, o => o.MapFrom(p => p.Description.MarketItem != null ? (int?)p.Description.MarketItem.Supply : null))
                .ForMember(x => x.Demand, o => o.MapFrom(p => p.Description.MarketItem != null ? (int?)p.Description.MarketItem.Demand : null))
                .ForMember(x => x.BuyAskingPrice, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.BuyAskingPrice : null, p => p.Description.MarketItem.Currency))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.BuyNowPrice : null, p => p.Description.MarketItem.Currency))
                .ForMember(x => x.Last24hrSales, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.Last24hrSales : null, p => p.Description.MarketItem.Currency));

            CreateMap<SteamMarketItem, InventoryMarketItemDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyAskingPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyAskingPrice, p => p.Currency))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyNowPrice, p => p.Currency))
                .ForMember(x => x.ResellPrice, o => o.MapFromUsingCurrencyExchange(p => p.ResellPrice, p => p.Currency))
                .ForMember(x => x.ResellTax, o => o.MapFromUsingCurrencyExchange(p => p.ResellTax, p => p.Currency))
                .ForMember(x => x.ResellProfit, o => o.MapFromUsingCurrencyExchange(p => p.ResellProfit, p => p.Currency))
                .ForMember(x => x.Last1hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last1hrValue, p => p.Currency));

            CreateMap<SteamMarketItem, MarketItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.SteamDescriptionId, o => o.MapFrom(p => p.Description.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyExchange(p => p.Description.StoreItem != null ? (long?) p.Description.StoreItem.Price : null, p => p.Description.StoreItem.Currency))
                .ForMember(x => x.BuyAskingPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyAskingPrice, p => p.Currency))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyNowPrice, p => p.Currency))
                .ForMember(x => x.ResellPrice, o => o.MapFromUsingCurrencyExchange(p => p.ResellPrice, p => p.Currency))
                .ForMember(x => x.ResellTax, o => o.MapFromUsingCurrencyExchange(p => p.ResellTax, p => p.Currency))
                .ForMember(x => x.ResellProfit, o => o.MapFromUsingCurrencyExchange(p => p.ResellProfit, p => p.Currency))
                .ForMember(x => x.Last1hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last1hrValue, p => p.Currency))
                .ForMember(x => x.Last24hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last24hrValue, p => p.Currency))
                .ForMember(x => x.Last48hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last48hrValue, p => p.Currency))
                .ForMember(x => x.Last72hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last72hrValue, p => p.Currency))
                .ForMember(x => x.Last120hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last120hrValue, p => p.Currency))
                .ForMember(x => x.Last336hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last336hrValue, p => p.Currency))
                .ForMember(x => x.MovementLast48hrValue, o => o.MapFromUsingCurrencyExchange(p => p.MovementLast48hrValue, p => p.Currency))
                .ForMember(x => x.MovementLast120hrValue, o => o.MapFromUsingCurrencyExchange(p => p.MovementLast120hrValue, p => p.Currency))
                .ForMember(x => x.MovementLast336hrValue, o => o.MapFromUsingCurrencyExchange(p => p.MovementLast336hrValue, p => p.Currency))
                .ForMember(x => x.AllTimeAverageValue, o => o.MapFromUsingCurrencyExchange(p => p.AllTimeAverageValue, p => p.Currency))
                .ForMember(x => x.AllTimeHighestValue, o => o.MapFromUsingCurrencyExchange(p => p.AllTimeHighestValue, p => p.Currency))
                .ForMember(x => x.AllTimeLowestValue, o => o.MapFromUsingCurrencyExchange(p => p.AllTimeLowestValue, p => p.Currency))
                .ForMember(x => x.MarketAge, o => o.MapFrom(p => p.MarketAge.ToMarketAgeString()))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile != null ? (int?) p.Description.WorkshopFile.Subscriptions : null));

            CreateMap<SteamMarketItem, MarketItemDetailDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconLargeUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile != null ? (int?)p.Description.WorkshopFile.Subscriptions : null))
                .ForMember(x => x.Favourited, o => o.MapFrom(p => p.Description.WorkshopFile != null ? (int?)p.Description.WorkshopFile.Favourited : null))
                .ForMember(x => x.Views, o => o.MapFrom(p => p.Description.WorkshopFile != null ? (int?)p.Description.WorkshopFile.Views : null))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));
            CreateMap<SteamMarketItemActivity, MarketItemActivityDTO>()
                .ForMember(x => x.Movement, o => o.MapFromUsingCurrencyExchange(p => p.Movement, p => p.Item.Currency));
            CreateMap<SteamMarketItemActivity, ProfileInventoryActivityDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Item.Description.Name))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Item.Description.IconLargeUrl))
                .ForMember(x => x.Movement, o => o.MapFromUsingCurrencyExchange(p => p.Movement, p => p.Item.Currency));
            CreateMap<SteamMarketItemOrder, MarketItemOrderDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));
            CreateMap<SteamMarketItemSale, MarketItemSaleDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));

            CreateMap<SteamItemStore, ItemStoreListDTO>();
            CreateMap<SteamItemStore, ItemStoreDetailedDTO>();

            CreateMap<GetSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>()
                .ForMember(x => x.Currency, o => o.MapFromCurrency());
                
            CreateMap<SteamStoreItemItemStore, StoreItemDetailDTO>()
                .ForMember(x => x.SteamId, o => o.MapFrom(p => p.Item.SteamId))
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.Item.App.SteamId))
                .ForMember(x => x.SteamWorkshopId, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null ? p.Item.Description.WorkshopFile.SteamId : null))
                .ForMember(x => x.SteamMarketItemId, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.SteamId : null))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Item.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Item.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Item.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Item.Description.IconUrl))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Item.Description.Tags.GetItemType(p.Item.Description.Name)))
                .ForMember(x => x.AuthorName, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null && p.Item.Description.WorkshopFile.Creator != null ? p.Item.Description.WorkshopFile.Creator.Name : p.Item.App.Name))
                .ForMember(x => x.AuthorAvatarUrl, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null && p.Item.Description.WorkshopFile.Creator != null ? p.Item.Description.WorkshopFile.Creator.AvatarUrl : p.Item.App.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.IsStillAvailableInStore, o => o.MapFrom(p => (p.Store != null && p.Store.End == null)))
                .ForMember(x => x.StorePrice, o => o.MapFrom(
                    (src, dst, _, context) =>
                    {
                        var currency = context.Items.ContainsKey(AutoMapperConfigurationExtensions.ContextKeyCurrency)
                            ? (CurrencyDetailedDTO)context.Options.Items[AutoMapperConfigurationExtensions.ContextKeyCurrency]
                            : null;
                        return (currency != null && src.Item.Prices.ContainsKey(currency.Name))
                            ? src.Item.Prices[currency.Name]
                            : 0;
                    }
                ))
                .ForMember(x => x.StoreIndex, o => o.MapFrom(p => p.Index))
                .ForMember(x => x.StoreIndexHistory, o => o.MapFrom(p => p.IndexGraph.ToHourlyGraphDictionary()))
                .ForMember(x => x.MarketPrice, o => o.MapFromUsingCurrencyExchange(p => p.Item.Description.MarketItem != null ? (long?) p.Item.Description.MarketItem.BuyNowPrice : null, p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.Currency : null))
                .ForMember(x => x.MarketQuantity, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? (int?) p.Item.Description.MarketItem.Supply : null))
                .ForMember(x => x.TotalSalesMin, o => o.MapFrom(p => p.Item.TotalSalesMin))
                .ForMember(x => x.TotalSalesMax, o => o.MapFrom(p => p.Item.TotalSalesMax))
                .ForMember(x => x.TotalSalesHistory, o => o.MapFrom(p => p.Item.TotalSalesGraph.ToDailyGraphDictionary()))
                .ForMember(x => x.SubscriptionsHistory, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null ? p.Item.Description.WorkshopFile.SubscriptionsGraph.ToDailyGraphDictionary() : null))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null ? (int?) p.Item.Description.WorkshopFile.Subscriptions : null))
                .ForMember(x => x.Favourited, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null ? (int?) p.Item.Description.WorkshopFile.Favourited : null))
                .ForMember(x => x.Views, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null ? (int?) p.Item.Description.WorkshopFile.Views : null))
                .ForMember(x => x.AcceptedOn, o => o.MapFrom(p => p.Item.Description.WorkshopFile != null ? p.Item.Description.WorkshopFile.AcceptedOn : null))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Item.Description.Tags.WithoutWorkshopTags()));
        }
    }
}
