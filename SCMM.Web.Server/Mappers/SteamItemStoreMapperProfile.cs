using AngleSharp.Common;
using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Store;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.Mappers
{
    public class SteamItemStoreMapperProfile : Profile
    {
        public SteamItemStoreMapperProfile()
        {
            var config = new ConfigurationManager().AddJsonFile("appsettings.json").Build();

            CreateMap<SteamItemStore, StoreIdentifierDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.StoreId()));

            CreateMap<SteamItemStore, StoreDetailsDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.StoreId()))
                .ForMember(x => x.IsDraft, o => o.MapFrom(p => p.IsDraft));

            CreateMap<SteamStoreItemItemStore, StoreItemDetailsDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Item.Id))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Item.SteamId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.Item.App.SteamId))
                .ForMember(x => x.AssetDescriptionId, o => o.MapFrom(p => p.Item.Description.ClassId))
                .ForMember(x => x.ItemDefinitionId, o => o.MapFrom(p => p.Item.Description.ItemDefinitionId))
                .ForMember(x => x.WorkshopFileId, o => o.MapFrom(p => p.Item.Description.WorkshopFileId))
                .ForMember(x => x.WorkshopFileUrl, o => o.MapFrom(p => p.Item.Description.WorkshopFileUrl))
                .ForMember(x => x.MarketListingId, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.SteamId : null))
                .ForMember(x => x.CreatorId, o => o.MapFrom(p => p.Item.Description.CreatorProfile != null ? p.Item.Description.CreatorProfile.SteamId : p.Item.App.SteamId))
                .ForMember(x => x.CreatorName, o => o.MapFrom(p => p.Item.Description.CreatorProfile != null ? p.Item.Description.CreatorProfile.Name : p.Item.App.Name))
                .ForMember(x => x.CreatorAvatarUrl, o => o.MapFrom(p => p.Item.Description.CreatorProfile != null ? p.Item.Description.CreatorProfile.AvatarUrl : p.Item.App.IconUrl))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Item.Description.ItemType))
                .ForMember(x => x.ItemCollection, o => o.MapFrom(p => p.Item.Description.ItemCollection))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Item.Description.Name))
                .ForMember(x => x.Description, o => o.MapFrom(p => p.Item.Description.Description))
                .ForMember(x => x.IsPermanent, o => o.MapFrom(p => p.Item.Description.IsPermanent))
                .ForMember(x => x.HasGlow, o => o.MapFrom(p => p.Item.Description.HasGlow))
                .ForMember(x => x.HasGlowSights, o => o.MapFrom(p => p.Item.Description.HasGlowSights))
                .ForMember(x => x.GlowRatio, o => o.MapFrom(p => Math.Round((p.Item.Description.GlowRatio ?? 0) * 100, 0)))
                .ForMember(x => x.HasCutout, o => o.MapFrom(p => p.Item.Description.HasCutout))
                .ForMember(x => x.CutoutRatio, o => o.MapFrom(p => Math.Round((p.Item.Description.CutoutRatio ?? 0) * 100, 0)))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Item.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Item.Description.ForegroundColour))
                .ForMember(x => x.IconAccentColour, o => o.MapFrom(p => p.Item.Description.IconAccentColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Item.Description.IconUrl))
                .ForMember(x => x.TimeCreated, o => o.MapFrom(p => p.Item.Description.TimeCreated))
                .ForMember(x => x.TimeAccepted, o => o.MapFrom(p => p.Item.Description.TimeAccepted))
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyTable(p => p.Prices))
                .ForMember(x => x.StorePriceUsd, o => o.MapFrom(p => p.Price))
                .ForMember(x => x.TopSellerIndex, o => o.MapFrom(p => p.TopSellerIndex))
                .ForMember(x => x.IsStillAvailableFromStore, o => o.MapFrom(p => p.Item != null ? p.Item.IsAvailable : false))
                .ForMember(x => x.HasReturnedToStoreBefore, o => o.MapFrom(p => p.Item != null ? p.Item.HasReturnedToStore : false))
                .ForMember(x => x.MarketPrice, o => o.MapFromUsingCurrencyExchange(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.SellOrderLowestPrice : null, p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.Currency : null))
                .ForMember(x => x.MarketSupply, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? (long?)p.Item.Description.MarketItem.SellOrderCount : null))
                .ForMember(x => x.MarketDemand24hrs, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? (long?)p.Item.Description.MarketItem.Last24hrSales : null))
                .ForMember(x => x.SupplyTotalEstimated, o => o.MapFrom(p => p.Item.Description.SupplyTotalEstimated))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Item.Description.SubscriptionsCurrent))
                .ForMember(x => x.IsCommodity, o => o.MapFrom(p => p.Item.Description.IsCommodity))
                .ForMember(x => x.IsMarketable, o => o.MapFrom(p => p.Item.Description.IsMarketable))
                .ForMember(x => x.MarketableRestrictionDays, o => o.MapFrom(p => p.Item.Description.MarketableRestrictionDays))
                .ForMember(x => x.IsTradable, o => o.MapFrom(p => p.Item.Description.IsTradable))
                .ForMember(x => x.TradableRestrictionDays, o => o.MapFrom(p => p.Item.Description.TradableRestrictionDays))
                .ForMember(x => x.IsSpecialDrop, o => o.MapFrom(p => p.Item.Description.IsSpecialDrop))
                .ForMember(x => x.IsBreakable, o => o.MapFrom(p => p.Item.Description.IsBreakable))
                .ForMember(x => x.BreaksIntoComponents, o => o.MapFrom(p => p.Item.Description.BreaksIntoComponents.ToDictionary(x => x.Key, x => x.Value)))
                .ForMember(x => x.IsBanned, o => o.MapFrom(p => p.Item.Description.IsBanned))
                .ForMember(x => x.BanReason, o => o.MapFrom(p => p.Item.Description.BanReason))
                .ForMember(x => x.Notes, o => o.MapFrom(p => p.Item.Description.Notes))
                .ForMember(x => x.IsDraft, o => o.MapFrom(p => p.IsDraft));

            CreateMap<SteamStoreItemItemStore, ItemStoreInstanceDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Store.StoreId()))
                .ForMember(x => x.Date, o => o.MapFrom(p => p.Store.Start))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Store.Name))
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyTable(p => p.Prices));
        }
    }
}
