using AutoMapper;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Profile;
using SCMM.Web.Data.Models.UI.Profile.Inventory;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.Mappers
{
    public class SteamProfileMapperProfile : Profile
    {
        public SteamProfileMapperProfile()
        {
            CreateMap<SteamProfile, MyProfileDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Language, o => o.MapFromLanguage())
                .ForMember(x => x.App, o => o.MapFromApp());
            CreateMap<SteamProfile, ProfileDTO>();
            CreateMap<SteamProfile, ProfileDetailedDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id));

            CreateMap<CalculateSteamProfileInventoryTotalsResponse, ProfileInventoryTotalsDTO>();

            CreateMap<SteamProfileInventoryItem, ProfileInventoryInvestmentItemDTO>()
                .ForMember(x => x.ItemId, o => o.MapFrom(p => p.SteamId))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Description.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.Description.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Description.ItemType))
                .ForMember(x => x.HasGlow, o => o.MapFrom(p => p.Description.HasGlow))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconAccentColour, o => o.MapFrom(p => p.Description.IconAccentColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.TimeCreated, o => o.MapFrom(p => p.Description.TimeCreated))
                .ForMember(x => x.TimeAccepted, o => o.MapFrom(p => p.Description.TimeAccepted))
                .ForMember(x => x.AcquiredBy, o => o.MapFrom(p => p.AcquiredBy))
                .ForMember(x => x.BuyCurrency, o => o.MapFrom(p => p.Currency))
                .ForMember(x => x.BuyPrice, o => o.MapFrom(p => p.BuyPrice))
                .ForMember(x => x.BuyPriceLocal, o => o.MapFromUsingCurrencyExchange(p => p.BuyPrice, p => p.Currency))
                .ForMember(x => x.BuyPriceStore, o => o.MapFromUsingCurrencyTable(p => p.Description.StoreItem != null ? p.Description.StoreItem.Prices : null))
                .ForMember(x => x.BuyPriceText, o => o.MapFrom(p => p.Currency != null && p.BuyPrice != null ? p.Currency.ToPriceString(p.BuyPrice.Value, true) : null))
                .ForMember(x => x.Quantity, o => o.MapFrom(p => p.Quantity))
                .ForMember(x => x.SellLaterTo, o => o.MapFrom(p => p.Description.MarketItem != null ? p.Description.MarketItem.SellLaterTo : Steam.Data.Models.Enums.MarketType.Unknown))
                .ForMember(x => x.SellLaterPrice, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.SellLaterPrice : null, p => p.Description.MarketItem.Currency))
                .ForMember(x => x.SellLaterFee, o => o.MapFromUsingCurrencyExchange(p => p.Description.MarketItem != null ? (long?)p.Description.MarketItem.SellLaterFee : null, p => p.Description.MarketItem.Currency));

            CreateMap<SteamProfileInventoryItem, ProfileInventoryItemDescriptionStackDTO>();
        }
    }
}
