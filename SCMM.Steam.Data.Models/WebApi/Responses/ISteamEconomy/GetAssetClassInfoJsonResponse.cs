using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamEconomy
{
    public class GetAssetClassInfoJsonResponse : Dictionary<string, JsonElement>
    {
        public bool Success => this["success"].Deserialize<bool>();

        public IEnumerable<AssetClassInfo> Assets => this.Values
            .Where(x => x.ValueKind == JsonValueKind.Object)
            .Select(x => x.Deserialize<AssetClassInfo>())
            .Where(x => x != null);
    }
}
