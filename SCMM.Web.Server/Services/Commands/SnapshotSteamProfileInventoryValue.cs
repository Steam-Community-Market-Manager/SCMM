using CommandQuery;
using SCMM.Data.Shared.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Models.Steam;
using SCMM.Web.Server.Services.Queries;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands
{
    public class SnapshotSteamProfileInventoryValueRequest : ICommand
    {
        public string ProfileId { get; set; }

        public string CurrencyId { get; set; }

        public GetSteamProfileInventoryTotalsResponse InventoryTotals { get; set; }
    }

    public class SnapshotSteamProfileInventoryValue : ICommandHandler<SnapshotSteamProfileInventoryValueRequest>
    {
        private readonly SteamDbContext _db;
        private readonly IQueryProcessor _queryProcessor;

        public SnapshotSteamProfileInventoryValue(SteamDbContext db, IQueryProcessor queryProcessor)
        {
            _db = db;
            _queryProcessor = queryProcessor;
        }

        public async Task HandleAsync(SnapshotSteamProfileInventoryValueRequest request)
        {
            if (request.InventoryTotals == null)
            {
                return;
            }

            // Resolve the id
            var resolvedId = await _queryProcessor.ProcessAsync(new ResolveSteamIdRequest()
            {
                Id = request.ProfileId
            });

            // Load the profile
            var profile = _db.SteamProfiles.Find(resolvedId.Id);
            if (profile == null)
            {
                return;
            }

            // Load the currency
            var currency = _db.SteamCurrencies.FirstOrDefault(x => x.SteamId == request.CurrencyId);
            if (currency == null)
            {
                return;
            }

            // Snapshot the inventory value if it has been more than an hour since the last snapshot
            profile.LastSnapshotInventoryOn = DateTimeOffset.Now;
            profile.InventorySnapshots.Add(new SteamProfileInventorySnapshot()
            {
                Profile = profile,
                Timestamp = DateTimeOffset.UtcNow,
                Currency = currency,
                InvestedValue = currency.CalculateExchange(request.InventoryTotals.TotalInvested ?? 0),
                MarketValue = currency.CalculateExchange(request.InventoryTotals.TotalMarketValue),
                TotalItems = request.InventoryTotals.TotalItems
            });
        }
    }
}
