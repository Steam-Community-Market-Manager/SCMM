using AngleSharp.Common;
using AutoMapper;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Store;
using SCMM.Web.Server.Extensions;
using System.Linq;

namespace SCMM.Web.Server.Mappers
{
    public class SteamItemStoreMapperProfile : Profile
    {
        public SteamItemStoreMapperProfile()
        {
            CreateMap<SteamItemStore, StoreIdentiferDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Start.UtcDateTime.AddMinutes(1).ToString(Constants.SCMMStoreIdDateFormat)));

            CreateMap<SteamItemStore, StoreDetailsDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Start.UtcDateTime.AddMinutes(1).ToString(Constants.SCMMStoreIdDateFormat)))
                .ForMember(x => x.IsDraft, o => o.MapFrom(p => p.IsDraft));

            CreateMap<SteamStoreItemItemStore, StoreItemDetailsDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Item.SteamId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.Item.App.SteamId))
                .ForMember(x => x.AssetDescriptionId, o => o.MapFrom(p => p.Item.Description.ClassId))
                .ForMember(x => x.WorkshopFileId, o => o.MapFrom(p => p.Item.Description.WorkshopFileId))
                .ForMember(x => x.MarketListingId, o => o.MapFrom(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.SteamId : null))
                .ForMember(x => x.AuthorName, o => o.MapFrom(p => p.Item.Description.Creator != null ? p.Item.Description.Creator.Name : p.Item.App.Name))
                .ForMember(x => x.AuthorAvatarUrl, o => o.MapFrom(p => p.Item.Description.Creator != null ? p.Item.Description.Creator.AvatarUrl : p.Item.App.IconUrl))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Item.Description.ItemType))
                .ForMember(x => x.ItemCollection, o => o.MapFrom(p => p.Item.Description.ItemCollection))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Item.Description.Name))
                .ForMember(x => x.Description, o => o.MapFrom(p => p.Item.Description.Description))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Item.Description.Tags))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Item.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Item.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Item.Description.IconUrl))
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyTable(p => p.Item != null ? p.Item.Prices : null))
                .ForMember(x => x.TopSellerIndex, o => o.MapFrom(p => p.TopSellerIndex))
                .ForMember(x => x.IsStillAvailableFromStore, o => o.MapFrom(p => p.Item != null ? p.Item.IsAvailable : false))
                .ForMember(x => x.MarketPrice, o => o.MapFromUsingCurrencyExchange(p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.BuyNowPrice : null, p => p.Item.Description.MarketItem != null ? p.Item.Description.MarketItem.Currency : null))
                .ForMember(x => x.SalesMinimum, o => o.MapFrom(p => p.Item.TotalSalesMin))
                .ForMember(x => x.SalesMaximum, o => o.MapFrom(p => p.Item.TotalSalesMax))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Item.Description.LifetimeSubscriptions))
                .ForMember(x => x.IsMarketable, o => o.MapFrom(p => p.Item.Description.IsMarketable))
                .ForMember(x => x.MarketableRestrictionDays, o => o.MapFrom(p => p.Item.Description.MarketableRestrictionDays))
                .ForMember(x => x.IsTradable, o => o.MapFrom(p => p.Item.Description.IsTradable))
                .ForMember(x => x.TradableRestrictionDays, o => o.MapFrom(p => p.Item.Description.TradableRestrictionDays))
                .ForMember(x => x.IsBreakable, o => o.MapFrom(p => p.Item.Description.IsBreakable))
                .ForMember(x => x.BreaksIntoComponents, o => o.MapFrom(p => p.Item.Description.BreaksIntoComponents.ToDictionary(x => x.Key, x => x.Value)))
                .ForMember(x => x.IsDraft, o => o.MapFrom(p => p.IsDraft));
        }
    }
}
