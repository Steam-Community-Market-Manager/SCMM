using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Domain.Models.Steam;
using System.Diagnostics;

namespace SCMM.Web.Server.Data
{
    public class SteamDbContext : DbContext
    {
        public static readonly ILoggerFactory DebugLoggerFactory =
            LoggerFactory.Create(builder => {
                builder.AddDebug();
            });

        public DbSet<SteamLanguage> SteamLanguages { get; set; }
        public DbSet<SteamCurrency> SteamCurrencies { get; set; }
        public DbSet<SteamProfile> SteamProfiles { get; set; }
        public DbSet<SteamApp> SteamApps { get; set; }
        public DbSet<SteamStoreItem> SteamStoreItems { get; set; }
        public DbSet<SteamMarketItem> SteamMarketItems { get; set; }
        public DbSet<SteamInventoryItem> SteamInventoryItems { get; set; }
        public DbSet<SteamAssetDescription> SteamAssetDescriptions { get; set; }
        public DbSet<SteamAssetWorkshopFile> SteamAssetWorkshopFiles { get; set; }

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

            builder.Entity<SteamProfile>()
                .HasMany(x => x.InventoryItems)
                .WithOne(x => x.Owner);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.WorkshopFiles)
                .WithOne(x => x.Creator);

            builder.Entity<SteamApp>()
                .OwnsMany(x => x.Filters)
                .OwnsOne(x => x.Options);
            builder.Entity<SteamApp>()
                .HasMany(x => x.Assets)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.WorkshopFiles)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.StoreItems)
                .WithOne(x => x.App);
            builder.Entity<SteamApp>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.App);

            builder.Entity<SteamStoreItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamStoreItem>()
                .HasOne(x => x.Description);

            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.BuyOrders)
                .WithOne(x => x.Item);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SellOrders)
                .WithOne(x => x.Item);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SalesHistory)
                .WithOne(x => x.Item);

            builder.Entity<SteamInventoryItem>()
                .HasOne(x => x.App);
            builder.Entity<SteamInventoryItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamInventoryItem>()
                .HasOne(x => x.Description);

            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.WorkshopFile);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Tags);

            builder.Entity<SteamAssetWorkshopFile>()
                .OwnsOne(x => x.SubscriptionsGraph);
        }
    }
}
