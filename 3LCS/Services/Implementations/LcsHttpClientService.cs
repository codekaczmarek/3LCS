using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsHttpClientService : ILcsHttpClientService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LcsHttpClientService> _logger;
        private bool _disposed;

        public CookieContainer CookieContainer { get; }
        public HttpClient HttpClient => _httpClient;
        public string LcsProjectId { get; set; } = string.Empty;
        public ProjectType LcsProjectTypeId { get; set; }
        public string LcsUrl { get; }
        public string LcsUpdateUrl { get; }
        public string LcsDiagUrl { get; }

        public LcsHttpClientService(ISettingsService settings, CookieContainer cookieContainer, ILogger<LcsHttpClientService> logger)
        {
            _logger = logger;
            LcsUrl = settings.LcsUrl;
            LcsUpdateUrl = settings.LcsUpdateUrl;
            LcsDiagUrl = settings.LcsDiagUrl;
            CookieContainer = cookieContainer;

            var handler = new HttpClientHandler
            {
                CookieContainer = CookieContainer,
                AllowAutoRedirect = true,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3872.0 Safari/537.36 Edg/78.0.244.0");
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            _httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("TimezoneOffset", GetTimeZoneOffsetInMinutes());
            _httpClient.DefaultRequestHeaders.Referrer = new Uri(LcsUrl);

            _logger.LogInformation("LcsHttpClientService initialized. LcsUrl={LcsUrl}", LcsUrl);
        }

        public void ChangeLcsProjectId(string value)
        {
            _logger.LogInformation("Changing LCS project ID to {ProjectId}", value);
            LcsProjectId = value;
            var cookiesLcs = CookieContainer.GetCookies(new Uri(LcsUrl));
            var cookiesUpdateLcs = CookieContainer.GetCookies(new Uri(LcsUpdateUrl));
            foreach (Cookie cookie in cookiesLcs)
                if (cookie.Name == "lcspid") cookie.Expired = true;
            foreach (Cookie cookie in cookiesUpdateLcs)
                if (cookie.Name == "lcspid") cookie.Expired = true;

            if (string.IsNullOrEmpty(LcsProjectId)) return;
            CookieContainer.Add(new Uri(LcsUrl), new Cookie("lcspid", LcsProjectId));
            CookieContainer.Add(new Uri(LcsUpdateUrl), new Cookie("lcspid", LcsProjectId));
        }

        public IDisposable BeginProjectScope(int projectId, ProjectType projectTypeId)
        {
            var savedId = LcsProjectId;
            var savedType = LcsProjectTypeId;
            ChangeLcsProjectId(projectId.ToString());
            LcsProjectTypeId = projectTypeId;
            return new ProjectScope(() =>
            {
                ChangeLcsProjectId(savedId);
                LcsProjectTypeId = savedType;
            });
        }

        private sealed class ProjectScope : IDisposable
        {
            private readonly Action _restore;
            private bool _disposed;
            internal ProjectScope(Action restore) => _restore = restore;
            public void Dispose() { if (!_disposed) { _restore(); _disposed = true; } }
        }

        private static string GetTimeZoneOffsetInMinutes() =>
            (-TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)).TotalMinutes.ToString();

        public void Dispose()
        {
            if (!_disposed) { _httpClient.Dispose(); _disposed = true; }
        }
    }
}
