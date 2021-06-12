using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class SteamDbContext : DbContext
    {
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
        public DbSet<SteamProfileInventorySnapshot> SteamProfileInventorySnapshots { get; set; }
        public DbSet<SteamProfileMarketItem> SteamProfileMarketItems { get; set; }

        public DbSet<ImageData> ImageData { get; set; }

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
                .HasIndex(x => x.SteamId)
                .IsUnique(true);
            builder.Entity<SteamApp>()
                .HasOne(x => x.Icon);
            builder.Entity<SteamApp>()
                .HasMany(x => x.Filters)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamApp>()
                .HasMany(x => x.WorkshopFiles)
                .WithOne(x => x.App)
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
                .HasIndex(x => x.ClassId)
                .IsUnique(true);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.Tags);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.Icon);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.IconLarge);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.Image);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.CraftingComponents);
            builder.Entity<SteamAssetDescription>()
                .OwnsOne(x => x.BreaksIntoComponents);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.StoreItem)
                .WithOne(x => x.Description)
                .HasForeignKey<SteamStoreItem>(x => x.DescriptionId);
            builder.Entity<SteamAssetDescription>()
                .HasOne(x => x.MarketItem)
                .WithOne(x => x.Description)
                .HasForeignKey<SteamMarketItem>(x => x.DescriptionId);

            builder.Entity<SteamAssetWorkshopFile>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);
            builder.Entity<SteamAssetWorkshopFile>()
                .HasOne(x => x.Image);
            builder.Entity<SteamAssetWorkshopFile>()
                .OwnsOne(x => x.SubscriptionsGraph);

            builder.Entity<SteamCurrency>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);

            builder.Entity<SteamItemStore>()
                .HasIndex(x => new { x.AppId, x.Start, x.End })
                .IsUnique(true);
            builder.Entity<SteamItemStore>()
                .OwnsOne(x => x.Media);

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
                .HasMany(x => x.BuyOrders)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SellOrders)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.SalesHistory)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamMarketItem>()
                .HasMany(x => x.Activity)
                .WithOne(x => x.Item)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SteamProfile>()
                .HasIndex(x => x.SteamId)
                .IsUnique(true);
            builder.Entity<SteamProfile>()
                .HasIndex(x => x.DiscordId)
                .IsUnique(true);
            builder.Entity<SteamProfile>()
                .HasOne(x => x.Avatar);
            builder.Entity<SteamProfile>()
                .HasOne(x => x.Language);
            builder.Entity<SteamProfile>()
                .HasOne(x => x.Currency);
            builder.Entity<SteamProfile>()
                .OwnsOne(x => x.Roles);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.InventoryItems)
                .WithOne(x => x.Profile)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.InventorySnapshots)
                .WithOne(x => x.Profile)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.MarketItems)
                .WithOne(x => x.Profile)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.WorkshopFiles)
                .WithOne(x => x.Creator)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<SteamProfile>()
                .HasMany(x => x.Configurations)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SteamProfileConfiguration>()
                .OwnsOne(x => x.List);

            builder.Entity<SteamProfileInventoryItem>()
                .HasIndex(x => new { x.SteamId, x.DescriptionId, x.ProfileId })
                .IsUnique(true);
            builder.Entity<SteamProfileInventoryItem>()
                .HasOne(x => x.Description);
            builder.Entity<SteamProfileInventoryItem>()
                .HasOne(x => x.Currency);

            builder.Entity<SteamProfileInventorySnapshot>()
                .HasIndex(x => new { x.ProfileId, x.Timestamp })
                .IsUnique(true);
            builder.Entity<SteamProfileInventorySnapshot>()
                .HasOne(x => x.Currency);

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
            builder.Entity<SteamStoreItem>()
                .OwnsOne(x => x.TotalSalesGraph);

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
        }
    }
}
