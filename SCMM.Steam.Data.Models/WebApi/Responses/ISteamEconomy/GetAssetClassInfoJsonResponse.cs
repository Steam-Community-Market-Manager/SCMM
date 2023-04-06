using SCMM.Steam.Data.Models.WebApi.Models;
using System.Text.Json.Nodes;

namespace SCMM.Steam.Data.Models.WebApi.Responses.ISteamEconomy
{
    public class GetAssetClassInfoJsonResponse : Dictionary<string, JsonValue>
    {
        public bool Success => this["success"].GetValue<bool>();

        public IEnumerable<AssetClassInfo> Assets => this.Values
            .Select(x =>
            {
                AssetClassInfo result = null;
                x.TryGetValue<AssetClassInfo>(out result);
                return result;
            })
            .Where(x => x != null);
    }
}
