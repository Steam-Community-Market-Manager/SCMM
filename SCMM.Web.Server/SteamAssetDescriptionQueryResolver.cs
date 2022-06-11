using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;

namespace SCMM.Web.Server;

public class SteamAssetDescriptionQueryResolver
{
    [GraphQLDescription("Gets the list of asset descriptions")]
    [UsePaging(DefaultPageSize = 30, MaxPageSize = 100, IncludeTotalCount = true)]
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public async Task<IQueryable<SteamAssetDescription>> GetAssetDescriptions(SteamDbContext db)
    {
        return db.SteamAssetDescriptions.AsNoTracking().AsQueryable();
    }
}
