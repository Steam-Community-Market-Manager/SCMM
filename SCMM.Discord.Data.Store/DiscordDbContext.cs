using Microsoft.EntityFrameworkCore;

namespace SCMM.Discord.Data.Store
{
    public class DiscordDbContext : DbContext
    {
        public DbSet<DiscordGuild> DiscordGuilds { get; set; }
        public DbSet<DiscordUser> DiscordUsers { get; set; }

        public DiscordDbContext(DbContextOptions<DiscordDbContext> options)
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
                .ToContainer(nameof(DiscordGuild))
                .HasNoDiscriminator();
            builder.Entity<DiscordGuild>()
                .HasIndex(x => x.Id)
                .IsUnique(true);
            builder.Entity<DiscordGuild>()
                .OwnsMany(x => x.Configuration, guildConfigurationBuilder => {
                    guildConfigurationBuilder.OwnsOne(y => y.List);
                });

            builder.Entity<DiscordUser>()
                .ToContainer(nameof(DiscordUser))
                .HasNoDiscriminator();
            builder.Entity<DiscordUser>()
                .HasIndex(x => x.Id)
                .IsUnique(true);
            builder.Entity<DiscordUser>()
                .HasIndex(x => new { x.Username, x.Discriminator })
                .IsUnique(true);
        }
    }
}
