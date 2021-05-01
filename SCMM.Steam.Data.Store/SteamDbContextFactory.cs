using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SCMM.Steam.Data.Store
{
    public class SteamDbContextFactory : IDesignTimeDbContextFactory<SteamDbContext>
    {
        public SteamDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SteamDbContext>();
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=SCMM.Steam;Integrated Security=True");

            return new SteamDbContext(optionsBuilder.Options);
        }
    }
}
