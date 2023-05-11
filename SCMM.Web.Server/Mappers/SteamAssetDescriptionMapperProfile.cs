using AutoMapper;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Profile.Inventory;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.Mappers
{
    public class SteamAssetDescriptionMapperProfile : Profile
    {
        public SteamAssetDescriptionMapperProfile()
        {
            CreateMap<SteamAssetFilter, ItemFilterDTO>();

            CreateMap<SteamAssetDescription, ItemDetailedDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.CreatorId, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.SteamId : p.App.SteamId))
                .ForMember(x => x.CreatorName, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.Name : p.App.Name))
                .ForMember(x => x.CreatorAvatarUrl, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.AvatarUrl : p.App.IconUrl))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Tags.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value)))
                .ForMember(x => x.Changes, o => o.MapFrom(p => p.Changes.OrderByDescending(x => x.Key).ToDictionary(x => x.Key.Ticks, x => x.Value)))
                .ForMember(x => x.IsAvailableOnMarket, o => o.MapFrom(p => (p.MarketItem != null ? (p.MarketItem.SellOrderLowestPrice > 0) : false)))
                .ForMember(x => x.MarketId, o => o.MapFrom(p => p.NameHash))
                .ForMember(x => x.MarketBuyOrderCount, o => o.MapFrom(p => (p.MarketItem != null ? (int?)p.MarketItem.BuyOrderCount : null)))
                .ForMember(x => x.MarketBuyOrderHighestPrice, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.BuyOrderHighestPrice : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketSellOrderCount, o => o.MapFrom(p => (p.MarketItem != null ? (int?)p.MarketItem.SellOrderCount : null)))
                .ForMember(x => x.MarketSellOrderLowestPrice, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.SellOrderLowestPrice : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.Market1hrSales, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last1hrSales : null)))
                .ForMember(x => x.Market1hrValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.Last1hrValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.Market24hrSales, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrSales : null)))
                .ForMember(x => x.Market24hrValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketLastSaleValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.LastSaleValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketLastSaleOn, o => o.MapFrom(p => (p.MarketItem != null ? p.MarketItem.LastSaleOn : null)))
                .ForMember(x => x.MarketHighestValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.AllTimeHighestValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketLowestValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.AllTimeLowestValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.TimeMarketHighestValue, o => o.MapFrom(p => (p.MarketItem != null ? p.MarketItem.AllTimeHighestValueOn : null)))
                .ForMember(x => x.TimeMarketLowestValue, o => o.MapFrom(p => (p.MarketItem != null ? p.MarketItem.AllTimeLowestValueOn : null)))
                .ForMember(x => x.TimeMarketFirstSold, o => o.MapFrom(p => (p.MarketItem != null ? p.MarketItem.FirstSaleOn : null)))
                .ForMember(x => x.IsBeingManipulated, o => o.MapFrom(p => (p.MarketItem != null ? p.MarketItem.IsBeingManipulated : false)))
                .ForMember(x => x.ManipulationReason, o => o.MapFrom(p => (p.MarketItem != null ? p.MarketItem.ManipulationReason : null)))
                .ForMember(x => x.IsAvailableOnStore, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.IsAvailable : false)))
                .ForMember(x => x.HasReturnedToStoreBefore, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.HasReturnedToStore : false)))
                .ForMember(x => x.StoreId, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.SteamId : null)))
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyTable(p => (p.StoreItem != null ? p.StoreItem.Prices : null)))
                .ForMember(x => x.Stores, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.Stores : null)))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.MarketType))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Price))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Url))
                .ForMember(x => x.BuyPrices, o => o.MapFromAssetBuyPrices(p => p))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.GetInteractions()));

            CreateMap<SteamAssetDescription, ItemDescriptionDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId));

            CreateMap<SteamAssetDescription, ItemDescriptionWithPriceDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.OriginalPrice, o => o.MapFromUsingCurrencyTable(p => (p.StoreItem != null ? p.StoreItem.Prices : null)))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.MarketType))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Price))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Url))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.SubscriptionsCurrent))
                .ForMember(x => x.SupplyTotalEstimated, o => o.MapFrom(p => p.SupplyTotalEstimated))
                .ForMember(x => x.Supply, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.SellOrderCount : null)))
                .ForMember(x => x.Demand, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrSales : null)))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.GetInteractions()));

            CreateMap<SteamAssetDescription, ProfileInventoryItemDescriptionDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.OriginalPrice, o => o.MapFromUsingCurrencyTable(p => (p.StoreItem != null ? p.StoreItem.Prices : null)))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.MarketType))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Price))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Url))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.SubscriptionsCurrent))
                .ForMember(x => x.SupplyTotalEstimated, o => o.MapFrom(p => p.SupplyTotalEstimated))
                .ForMember(x => x.Supply, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.SellOrderCount : null)))
                .ForMember(x => x.Demand, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrSales : null)))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.GetInteractions()));

            CreateMap<SteamAssetDescription, ItemBestMarketPriceDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Name))
                .ForMember(x => x.Price, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Price))
                .ForMember(x => x.Supply, o => o.MapFromUsingAssetBuyPrice(p => p, p => p.Supply));

            CreateMap<SteamAssetDescription, ItemMarketPricesDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Name))
                .ForMember(x => x.Prices, o => o.MapFromAssetBuyPrices(p => p));

            CreateMap<MarketPrice, ItemBasicMarketPriceDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Currency));

            CreateMap<MarketPrice, ItemMarketPriceDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Currency));

            CreateMap<ItemInteraction, ItemInteractionDTO>();
        }
    }
}
