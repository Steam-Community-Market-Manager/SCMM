using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data.Models.Discord;
using SCMM.Web.Server.Data.Models.Steam;

namespace SCMM.Web.Server.Data
{
    public class ScmmDbContext : DbContext
    {
        public static readonly ILoggerFactory DebugLoggerFactory =
            LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
            });

        public DbSet<DiscordGuild> DiscordGuilds { get; set; }

        public DbSet<SteamLanguage> SteamLanguages { get; set; }
        public DbSet<SteamCurrency> SteamCurrencies { get; set; }
        public DbSet<SteamApp> SteamApps { get; set; }
        public DbSet<SteamItemStore> SteamItemStores { get; set; }
        public DbSet<SteamStoreItem> SteamStoreItems { get; set; }
        public DbSet<SteamMarketItem> SteamMarketItems { get; set; }
        public DbSet<SteamMarketItemSale> SteamMarketItemSale { get; set; }
        public DbSet<SteamAssetDescription> SteamAssetDescriptions { get; set; }
        public DbSet<SteamAssetWorkshopFile> SteamAssetWorkshopFiles { get; set; }
        public DbSet<SteamProfile> SteamProfiles { get; set; }
        public DbSet<SteamProfileInventoryItem> SteamProfileInventoryItems { get; set; }
        public DbSet<SteamProfileMarketItem> SteamProfileMarketItems { get; set; }

        public ScmmDbContext(DbContextOptions<ScmmDbContext> options)
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
                .HasMany(x => x.Configurations);
            builder.Entity<DiscordConfiguration>()
                .OwnsOne(x => x.List);

            builder.Entity<SteamApp>()
                .OwnsMany(x => x.Filters)
                .OwnsOne(x => x.Options);
            builder.Entity<SteamApp>()
                .HasMany(x => x.WorkshopFiles)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.Assets)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.StoreItems)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.ItemStores)
                .WithOne(x => x.App);

            builder.Entity<SteamStoreItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamStoreItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamStoreItem>()
                .OwnsOne(x => x.Prices);
            builder.Entity<SteamStoreItem>()
                .OwnsOne(x => x.TotalSalesGraph);

            builder.Entity<SteamItemStore>()
                .OwnsOne(x => x.Media);

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
                .OwnsOne(x => x.IndexGraph);

            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.BuyOrders)
                .WithOne(x => x.Item);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SellOrders)
                .WithOne(x => x.Item);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SalesHistory)
                .WithOne(x => x.Item);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.Activity)
                .WithOne(x => x.Item);

            builder.Entity<SteamProfileInventoryItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamProfileInventoryItem>()
                .HasOne(x => x.Currency);

            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.WorkshopFile);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.StoreItem)
                .WithOne(x => x.Description)
                .HasForeignKey<SteamStoreItem>(x => x.DescriptionId);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.MarketItem)
                .WithOne(x => x.Description)
                .HasForeignKey<SteamMarketItem>(x => x.DescriptionId);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Tags);

            builder.Entity<SteamAssetWorkshopFile>()
                .OwnsOne(x => x.SubscriptionsGraph);

            builder.Entity<SteamProfile>()
                .HasOne(x => x.Language);
            builder.Entity<SteamProfile>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamProfile>()
                .OwnsOne(x => x.Roles);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.InventoryItems)
                .WithOne(x => x.Profile);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.Profile);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.WorkshopFiles)
                .WithOne(x => x.Creator);
            builder.Entity<SteamProfile>()
                 .HasMany(x => x.Configurations);
            builder.Entity<SteamProfileConfiguration>()
                .OwnsOne(x => x.List);
        }
    }
}
