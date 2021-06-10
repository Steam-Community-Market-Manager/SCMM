using AngleSharp.Common;
using AutoMapper;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.Domain.Currencies;
using SCMM.Web.Data.Models.Domain.InventoryItems;
using SCMM.Web.Data.Models.Domain.Languages;
using SCMM.Web.Data.Models.Domain.MarketItems;
using SCMM.Web.Data.Models.Domain.Profiles;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.ProfileInventory;
using SCMM.Web.Data.Models.UI.Store;
using SCMM.Web.Server.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            //
            // LANGUAGES
            //

            CreateMap<SteamLanguage, LanguageDTO>();
            CreateMap<SteamLanguage, LanguageListDTO>();
            CreateMap<SteamLanguage, LanguageDetailedDTO>();

            //
            // CURRENCIES
            //

            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamCurrency, CurrencyListDTO>();
            CreateMap<SteamCurrency, CurrencyDetailedDTO>();

            //
            // PROFILES
            //

            CreateMap<SteamProfile, ProfileDTO>();
            CreateMap<SteamProfile, ProfileDetailedDTO>();
            CreateMap<SteamProfile, ProfileSummaryDTO>();

            CreateMap<GetSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>()
                .ForMember(x => x.Currency, o => o.MapFromCurrency());

            CreateMap<SteamAssetDescription, ProfileInventoryItemSummaryDTO>()
                .ForMember(x => x.SteamId, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Flags, o => o.Ignore());

            CreateMap<SteamProfileInventoryItem, InventoryInvestmentItemDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Description.ItemType))
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
                .ForMember(x => x.SteamId, o => o.MapFrom(p => p.Description.ClassId.ToString()))
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

            CreateMap<SteamMarketItemActivity, ProfileInventoryActivityDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Item.Description.Name))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Item.Description.IconLargeUrl))
                .ForMember(x => x.Movement, o => o.MapFromUsingCurrencyExchange(p => p.Movement, p => p.Item.Currency));

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
            
            //
            // MARKET
            //

            CreateMap<SteamMarketItem, MarketItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.SteamDescriptionId, o => o.MapFrom(p => p.Description.ClassId.ToString()))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyExchange(p => p.Description.StoreItem != null ? (long?)p.Description.StoreItem.Price : null, p => p.Description.StoreItem.Currency))
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
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.TotalSubscriptions != null ? (int?)p.Description.TotalSubscriptions : null));

            CreateMap<SteamMarketItem, MarketItemDetailDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconLargeUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.TotalSubscriptions != null ? (int?)p.Description.TotalSubscriptions : null))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags));

            CreateMap<SteamMarketItemActivity, MarketItemActivityDTO>()
                .ForMember(x => x.Movement, o => o.MapFromUsingCurrencyExchange(p => p.Movement, p => p.Item.Currency));

            CreateMap<SteamMarketItemOrder, MarketItemOrderDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));

            CreateMap<SteamMarketItemSale, MarketItemSaleDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));

            //
            // ITEM
            //
            CreateMap<List<SteamAssetDescription>, ItemCollectionDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Count > 0 ? p.FirstOrDefault().ItemCollection : null))
                .ForMember(x => x.AuthorName, o => o.MapFrom(p => p.Count(x => x.Creator != null) > 0 ? p.FirstOrDefault(x => x.Creator != null).Creator.Name : null))
                .ForMember(x => x.AuthorAvatarUrl, o => o.MapFrom(p => p.Count(x => x.Creator != null) > 0 ? p.FirstOrDefault(x => x.Creator != null).Creator.AvatarUrl : null))
                .ForMember(x => x.BuyNowCurrency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.Count > 0 ? p.Sum(x => x.BuyNowPrice) : null, p => p.Count > 0 ? p.FirstOrDefault().BuyNowCurrency : null))
                .ForMember(x => x.Items, o => o.MapFrom(p => p));

            CreateMap<SteamAssetDescription, ItemDetailsDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.BuyNowCurrency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyNowPrice, p => p.BuyNowCurrency));

            //
            // STORE
            //

            CreateMap<SteamItemStore, StoreIdentiferDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Start.UtcDateTime.AddMinutes(1).ToString(Constants.SCMMStoreIdDateFormat)))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.GetFullName()));

            CreateMap<SteamItemStore, StoreDetailsDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Start.UtcDateTime.AddMinutes(1).ToString(Constants.SCMMStoreIdDateFormat)))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.GetFullName()));

            CreateMap<SteamStoreItemItemStore, StoreItemDetailsDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Item.SteamId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.Item.App.SteamId))
                .ForMember(x => x.AppName, o => o.MapFrom(p => p.Item.App.Name))
                .ForMember(x => x.WorkshopFileId, o => o.MapFrom(p => p.Item.Description.WorkshopFileId))
                .ForMember(x => x.MarketListingId, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.SteamId : null))
                .ForMember(x => x.AuthorName, o => o.MapFrom(p => p.Item.Description.Creator != null ? p.Item.Description.Creator.Name : p.Item.App.Name))
                .ForMember(x => x.AuthorAvatarUrl, o => o.MapFrom(p => p.Item.Description.Creator != null ? p.Item.Description.Creator.AvatarUrl : p.Item.App.IconUrl))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Item.Description.Name))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Item.Description.ItemType))
                .ForMember(x => x.ItemCollection, o => o.MapFrom(p => p.Item.Description.ItemCollection))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Item.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Item.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Item.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyTable(p => p.Item != null ? p.Item.Prices : null))
                .ForMember(x => x.StoreIndex, o => o.MapFrom(p => p.Index))
                .ForMember(x => x.IsStillAvailableInStore, o => o.MapFrom(p => p.Item != null ? p.Item.IsActive : false))
                .ForMember(x => x.MarketPrice, o => o.MapFromUsingCurrencyExchange(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.BuyNowPrice : null, p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.Currency : null))
                .ForMember(x => x.SalesMinimum, o => o.MapFrom(p => p.Item.TotalSalesMin))
                .ForMember(x => x.SalesMaximum, o => o.MapFrom(p => p.Item.TotalSalesMax))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Item.Description.TotalSubscriptions != null ? p.Item.Description.TotalSubscriptions : null))
                .ForMember(x => x.IsMarketable, o => o.MapFrom(p => p.Item.Description.IsMarketable))
                .ForMember(x => x.MarketableRestrictionDays, o => o.MapFrom(p => p.Item.Description.MarketableRestrictionDays))
                .ForMember(x => x.IsTradable, o => o.MapFrom(p => p.Item.Description.IsTradable))
                .ForMember(x => x.TradableRestrictionDays, o => o.MapFrom(p => p.Item.Description.TradableRestrictionDays))
                .ForMember(x => x.IsBreakable, o => o.MapFrom(p => p.Item.Description.IsBreakable))
                .ForMember(x => x.BreaksIntoComponents, o => o.MapFrom(p => p.Item.Description.BreaksIntoComponents.ToDictionary(x => x.Key, x => x.Value)));
        }
    }
}
