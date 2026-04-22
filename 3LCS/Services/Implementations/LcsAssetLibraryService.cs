using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsAssetLibraryService : LcsServiceBase, ILcsAssetLibraryService
    {
        public LcsAssetLibraryService(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, ILogger<LcsAssetLibraryService> logger)
            : base(http, sessionState, auth, logger) { }

        public List<Asset> GetSharedAssetList(AssetFileType assetFileType)
        {
            var url = $"{Http.LcsUrl}/FileAsset/GetSharedAssets/?assetKind={(int)assetFileType}&_={Ts()}";
            var response = GetResponseSync(url);
            if (response?.Success == true && response.Data is JToken token)
            {
                var assetData = token[0]?.ToObject<AssetData>();
                if (assetData?.Assets != null) return assetData.Assets;
            }
            return new List<Asset>();
        }

        public List<AssetVersion> GetSharedAssetVersionList(string parentFileAssetId)
        {
            var response = PostFormResponseSync(
                $"{Http.LcsUrl}/FileAsset/GetFileAssetVersions",
                $"parentFileAssetId={parentFileAssetId}");
            if (response?.Success == true && response.Data is JToken token)
                return token.ToObject<List<AssetVersion>>() ?? new();
            return new List<AssetVersion>();
        }

        public string GetAssetReleaseDetails(string releaseDetailsLink)
        {
            if (string.IsNullOrEmpty(releaseDetailsLink)) return string.Empty;
            var response = GetResponseSync($"{Http.LcsUrl}{releaseDetailsLink}&_={Ts()}");
            if (response?.Success == true && response.Data is JToken token)
            {
                var redirectLink = token["RedirectLink"]?.ToString();
                if (!string.IsNullOrEmpty(redirectLink))
                {
                    var html = Http.HttpClient.GetAsync(redirectLink).GetAwaiter().GetResult();
                    return html.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
            return string.Empty;
        }
    }
}
