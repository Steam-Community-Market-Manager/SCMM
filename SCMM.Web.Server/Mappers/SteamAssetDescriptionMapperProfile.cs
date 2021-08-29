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
            CreateMap<SteamAssetDescription, ItemDetailedDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.CreatorId, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.SteamId : p.App.SteamId))
                .ForMember(x => x.CreatorName, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.Name : p.App.Name))
                .ForMember(x => x.CreatorAvatarUrl, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.AvatarUrl : p.App.IconUrl))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetPrice(p => p, p => p.Type))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetPrice(p => p, p => p.BuyPrice))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetPrice(p => p, p => p.BuyUrl))
                .ForMember(x => x.IsAvailableOnMarket, o => o.MapFrom(p => (p.MarketItem != null ? true : false)))
                .ForMember(x => x.MarketId, o => o.MapFrom(p => p.NameHash))
                .ForMember(x => x.MarketBuyOrderCount, o => o.MapFrom(p => (p.MarketItem != null ? (int?)p.MarketItem.Demand : null)))
                .ForMember(x => x.MarketBuyPrice, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.BuyNowPrice : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketSellOrderCount, o => o.MapFrom(p => (p.MarketItem != null ? (int?)p.MarketItem.Supply : null)))
                .ForMember(x => x.MarketSellPrice, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.BuyAskingPrice : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketSellTax, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.ResellTax : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.Market1hrSales, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last1hrSales : null)))
                .ForMember(x => x.Market1hrValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.Last1hrValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.Market24hrSales, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrSales : null)))
                .ForMember(x => x.Market24hrValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.IsAvailableOnStore, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.IsAvailable : false)))
                .ForMember(x => x.HasReturnedToStoreBefore, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.HasReturnedToStore : false)))
                .ForMember(x => x.StoreId, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.SteamId : null)))
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyTable(p => (p.StoreItem != null ? p.StoreItem.Prices : null)))
                .ForMember(x => x.Stores, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.Stores : null)));

            CreateMap<List<SteamAssetDescription>, ItemCollectionDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Count > 0 ? p.FirstOrDefault().ItemCollection : null))
                .ForMember(x => x.CreatorName, o => o.MapFrom(p => p.Count(x => x.CreatorProfile != null) > 0 ? p.FirstOrDefault(x => x.CreatorProfile != null).CreatorProfile.Name : null))
                .ForMember(x => x.CreatorAvatarUrl, o => o.MapFrom(p => p.Count(x => x.CreatorProfile != null) > 0 ? p.FirstOrDefault(x => x.CreatorProfile != null).CreatorProfile.AvatarUrl : null))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.Count > 0 ? p.Sum(x => x[null].BuyPrice) : null, p => p.Count > 0 ? p.FirstOrDefault()[null].Currency : null))
                .ForMember(x => x.Items, o => o.MapFrom(p => p));

            CreateMap<SteamAssetDescription, ItemDescriptionDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId));

            CreateMap<SteamAssetDescription, ItemDescriptionWithPriceDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetPrice(p => p, p => p.Type))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetPrice(p => p, p => p.BuyPrice))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetPrice(p => p, p => p.BuyUrl))
                .ForMember(x => x.OriginalPrice, o => o.MapFromUsingCurrencyTable(p => (p.StoreItem != null ? p.StoreItem.Prices : null)))
                .ForMember(x => x.Supply, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Supply : null)))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.CurrentSubscriptions));

            CreateMap<SteamAssetDescription, ProfileInventoryItemDescriptionDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetPrice(p => p, p => p.BuyPrice))
                .ForMember(x => x.OriginalPrice, o => o.MapFromUsingCurrencyTable(p => (p.StoreItem != null ? p.StoreItem.Prices : null)))
                .ForMember(x => x.Supply, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Supply : null)))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.CurrentSubscriptions));

            CreateMap<Price, ItemPriceDTO>()
                .ForMember(x => x.BuyPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyPrice, p => p.Currency));
        }
    }
}
