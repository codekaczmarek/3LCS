using System.Threading.Tasks;

namespace ThreeLCS.Services.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Checks GitHub Releases for a newer version.
        /// Returns the latest release tag (e.g. "v1.2.3") when an update is available,
        /// or <see langword="null"/> when the app is already up-to-date or the check fails.
        /// </summary>
        Task<UpdateInfo?> CheckForUpdateAsync();
    }

    public sealed record UpdateInfo(string Version, string ReleaseUrl, string? InstallerUrl);
}
