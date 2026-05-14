using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public sealed class UpdateService : IUpdateService
    {
        private const string GitHubApiLatestRelease =
            "https://api.github.com/repos/codekaczmarek/3LCS/releases/latest";

        private readonly ILogger<UpdateService> _logger;

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
        }

        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                var currentVersion = GetCurrentVersion();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("3LCS", currentVersion.ToString()));
                client.Timeout = TimeSpan.FromSeconds(10);

                var json = await client.GetStringAsync(GitHubApiLatestRelease);
                var release = JObject.Parse(json);

                var tagName = release["tag_name"]?.Value<string>();
                var htmlUrl = release["html_url"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(htmlUrl))
                    return null;

                var latestVersion = ParseVersion(tagName);
                if (latestVersion is null || latestVersion <= currentVersion)
                    return null;

                // Try to find the installer asset first, fall back to self-contained zip.
                var installerUrl = FindAssetUrl(release, "-Setup.exe")
                    ?? FindAssetUrl(release, "-win-x64-self-contained.zip");

                return new UpdateInfo(tagName, htmlUrl, installerUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Update check failed");
                return null;
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static Version GetCurrentVersion()
        {
            var raw = Assembly.GetExecutingAssembly().GetName().Version;
            return raw ?? new Version(0, 0, 0);
        }

        private static Version? ParseVersion(string tag)
        {
            var stripped = tag.TrimStart('v');
            return Version.TryParse(stripped, out var v) ? v : null;
        }

        private static string? FindAssetUrl(JObject release, string suffix)
        {
            var assets = release["assets"] as JArray;
            if (assets is null) return null;

            return assets
                .OfType<JObject>()
                .FirstOrDefault(a =>
                    a["name"]?.Value<string>()?.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) == true)
                ?["browser_download_url"]
                ?.Value<string>();
        }
    }
}
