using Microsoft.EntityFrameworkCore;
using SCMM.Web.Server.Domain.Models.Steam;

namespace SCMM.Web.Server.Data
{
    public class SteamDbContext : DbContext
    {
        public DbSet<SteamLanguage> SteamLanguages { get; set; }
        public DbSet<SteamCurrency> SteamCurrencies { get; set; }
        public DbSet<SteamApp> SteamApps { get; set; }
        public DbSet<SteamItem> SteamItems { get; set; }

        public SteamDbContext(DbContextOptions<SteamDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<SteamApp>()
                .HasMany(x => x.Items)
                .WithOne(x => x.App);
            builder.Entity<SteamItem>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamItem>()
                .OwnsMany(x => x.BuyOrders);
            builder.Entity<SteamItem>()
                .OwnsMany(x => x.SellOrders);
            builder.Entity<SteamItemDescription>()
                .OwnsOne(x => x.Tags);
        }
    }
}
