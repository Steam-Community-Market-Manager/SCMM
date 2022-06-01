using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class SteamDbContext : DbContext
    {
        public DbSet<DiscordGuild> DiscordGuilds { get; set; }

        public DbSet<SteamLanguage> SteamLanguages { get; set; }
        public DbSet<SteamCurrency> SteamCurrencies { get; set; }
        public DbSet<SteamCurrencyExchangeRate> SteamCurrencyExchangeRates { get; set; }
        public DbSet<SteamApp> SteamApps { get; set; }
        public DbSet<SteamItemStore> SteamItemStores { get; set; }
        public DbSet<SteamStoreItemItemStore> SteamStoreItemItemStore { get; set; }
        public DbSet<SteamStoreItem> SteamStoreItems { get; set; }
        public DbSet<SteamStoreItemTopSellerPosition> SteamStoreItemTopSellerPositions { get; set; }
        public DbSet<SteamMarketItem> SteamMarketItems { get; set; }
        public DbSet<SteamMarketItemBuyOrder> SteamMarketItemBuyOrder { get; set; }
        public DbSet<SteamMarketItemSellOrder> SteamMarketItemSellOrder { get; set; }
        public DbSet<SteamMarketItemOrderSummary> SteamMarketItemOrderSummaries { get; set; }
        public DbSet<SteamMarketItemSale> SteamMarketItemSale { get; set; }
        public DbSet<SteamMarketItemActivity> SteamMarketItemActivity { get; set; }
        public DbSet<SteamAssetDescription> SteamAssetDescriptions { get; set; }
        public DbSet<SteamProfile> SteamProfiles { get; set; }
        public DbSet<SteamProfileInventoryItem> SteamProfileInventoryItems { get; set; }
        public DbSet<SteamProfileInventoryValue> SteamProfileInventoryValues { get; set; }
        public DbSet<SteamProfileMarketItem> SteamProfileMarketItems { get; set; }

        public DbSet<FileData> FileData { get; set; }

        public SteamDbContext(DbContextOptions<SteamDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseLoggerFactory(DebugLoggerFactory);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<DiscordGuild>()
                .HasIndex(x => x.DiscordId)
                .IsUnique(true);
            builder.Entity<DiscordGuild>()
                .HasMany(x => x.Configurations)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiscordConfiguration>()
                .OwnsOne(x => x.List);

            builder.Entity<SteamApp>()
                .HasKey(x => x.Id);
            builder.Entity<SteamApp>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);
            builder.Entity<SteamApp>()
                .HasMany(x => x.AssetFilters)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamApp>()
                .HasMany(x => x.AssetDescriptions)
                .WithOne(x => x.App)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamApp>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.App)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamApp>()
                .HasMany(x => x.StoreItems)
                .WithOne(x => x.App)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamApp>()
                .HasMany(x => x.ItemStores)
                .WithOne(x => x.App)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SteamAssetFilter>()
                .OwnsOne(x => x.Options);

            builder.Entity<SteamAssetDescription>()
                .HasIndex(x => new { x.ClassId, x.ItemDefinitionId })
                .IsUnique(true);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Notes);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Changes);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Tags);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.Icon);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Previews);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.CraftingComponents);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.BreaksIntoComponents);
            builder.Entity<SteamAssetDescription>()
                .HasMany(x => x.InventoryItems)
                .WithOne(x => x.Description)
                .HasForeignKey(x => x.DescriptionId);
            builder.Entity<SteamAssetDescription>()
                .HasMany(x => x.StoreItemTopSellerPositions)
                .WithOne(x => x.Description)
                .HasForeignKey(x => x.DescriptionId);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.StoreItem)
                .WithOne(x => x.Description)
                .HasForeignKey<SteamStoreItem>(x => x.DescriptionId);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.MarketItem)
                .WithOne(x => x.Description)
                .HasForeignKey<SteamMarketItem>(x => x.DescriptionId);

            builder.Entity<SteamCurrency>()
                .HasKey(x => x.Id);
            builder.Entity<SteamCurrency>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);

            builder.Entity<SteamCurrencyExchangeRate>()
                .HasKey(x => new { x.CurrencyId, x.Timestamp });

            builder.Entity<SteamItemStore>()
                .HasIndex(x => new { x.AppId, x.Start, x.End, x.Name })
                .IsUnique(true);
            builder.Entity<SteamItemStore>()
                .OwnsOne(x => x.Media);
            builder.Entity<SteamItemStore>()
                .OwnsOne(x => x.Notes);

            builder.Entity<SteamLanguage>()
                .HasKey(x => x.Id);
            builder.Entity<SteamLanguage>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);

            builder.Entity<SteamMarketItem>()
                .HasIndex(x => new { x.SteamId, x.DescriptionId })
                .IsUnique(true);
            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamMarketItem>()
                .OwnsOne(x => x.BuyPrices);
            builder.Entity<SteamMarketItem>()
                .OwnsOne(x => x.SellPrices);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.BuyOrders)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .OwnsOne(x => x.BuyOrderHighestPriceRolling24hrs);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SellOrders)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .OwnsOne(x => x.SellOrderLowestPriceRolling24hrs);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.OrdersHistory)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SalesHistory)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .OwnsOne(x => x.SalesPriceRolling24hrs);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.Activity)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SteamMarketItemActivity>()
                .HasIndex(x => new { x.Timestamp, x.DescriptionId, x.Type, x.Price, x.Quantity, x.BuyerName, x.SellerName })
                .IsUnique(true);
            builder.Entity<SteamMarketItemActivity>()
                .HasOne(x => x.Description);
            builder.Entity<SteamMarketItemActivity>()
                .HasOne(x => x.Item);
            builder.Entity<SteamMarketItemActivity>()
                .HasOne(x => x.Currency);

            builder.Entity<SteamProfile>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);
            builder.Entity<SteamProfile>()
                .HasIndex(x => x.DiscordId)
                .IsUnique(true);
            builder.Entity<SteamProfile>()
                .HasOne(x => x.Language);
            builder.Entity<SteamProfile>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamProfile>()
                .OwnsOne(x => x.Preferences);
            builder.Entity<SteamProfile>()
                .OwnsOne(x => x.Roles);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.InventoryItems)
                .WithOne(x => x.Profile)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.Profile)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.AssetDescriptions)
                .WithOne(x => x.CreatorProfile)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SteamProfileConfiguration>()
                .OwnsOne(x => x.List);

            builder.Entity<SteamProfileInventoryItem>()
                .HasIndex(x => new { x.SteamId, x.DescriptionId, x.ProfileId })
                .IsUnique(true);
            builder.Entity<SteamProfileInventoryItem>()
                .HasOne(x => x.Description)
                .WithMany(x => x.InventoryItems);
            builder.Entity<SteamProfileInventoryItem>()
                .HasOne(x => x.Currency);

            builder.Entity<SteamProfileInventoryValue>()
                .HasIndex(x => new { x.ProfileId, x.AppId })
                .IsUnique(true);

            builder.Entity<SteamProfileMarketItem>()
                .HasIndex(x => new { x.SteamId, x.DescriptionId, x.ProfileId })
                .IsUnique(true);

            builder.Entity<SteamStoreItem>()
                .HasIndex(x => new { x.SteamId, x.DescriptionId })
                .IsUnique(true);
            builder.Entity<SteamStoreItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamStoreItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamStoreItem>()
                .OwnsOne(x => x.Prices);

            builder.Entity<SteamStoreItemItemStore>()
                .HasKey(bc => new { bc.ItemId, bc.StoreId });
            builder.Entity<SteamStoreItemItemStore>()
                .HasOne(bc => bc.Item)
                .WithMany(b => b.Stores)
                .HasForeignKey(bc => bc.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<SteamStoreItemItemStore>()
                .HasOne(bc => bc.Store)
                .WithMany(b => b.Items)
                .HasForeignKey(bc => bc.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<SteamStoreItemItemStore>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamStoreItemItemStore>()
                .OwnsOne(x => x.Prices);

            builder.Entity<SteamStoreItemTopSellerPosition>()
                .HasIndex(x => new { x.Timestamp, x.DescriptionId, x.Position, x.Total })
                .IsUnique(true);
            builder.Entity<SteamStoreItemTopSellerPosition>()
                .HasOne(x => x.Description);
        }
    }
}
