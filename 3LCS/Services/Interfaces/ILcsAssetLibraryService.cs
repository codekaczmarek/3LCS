using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsAssetLibraryService
    {
        List<Asset> GetSharedAssetList(AssetFileType assetFileType);
        List<AssetVersion> GetSharedAssetVersionList(string parentFileAssetId);
        string GetAssetReleaseDetails(string releaseDetailsLink);
    }
}
