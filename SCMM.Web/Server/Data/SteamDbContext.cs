using Microsoft.EntityFrameworkCore;
using SCMM.Web.Server.Domain.Models.Steam;

namespace SCMM.Web.Server.Data
{
    public class SteamDbContext : DbContext
    {
        public DbSet<SteamLanguage> SteamLanguages { get; set; }
        public DbSet<SteamCurrency> SteamCurrencies { get; set; }
        public DbSet<SteamApp> SteamApps { get; set; }
        public DbSet<SteamMarketItem> SteamMarketItems { get; set; }
        public DbSet<SteamAssetDescription> SteamAssetDescriptions { get; set; }
        public DbSet<SteamAssetWorkshopFile> SteamAssetWorkshopFiles { get; set; }

        public SteamDbContext(DbContextOptions<SteamDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<SteamApp>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.App);
            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamMarketItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamMarketItem>()
                .OwnsMany(x => x.BuyOrders);
            builder.Entity<SteamMarketItem>()
                .OwnsMany(x => x.SellOrders);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.WorkshopFile);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Tags);
        }
    }
}
