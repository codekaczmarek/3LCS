using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsAuthService : ILcsAuthService
    {
        private readonly ILcsHttpClientService _http;
        private readonly ILcsSessionService _sessionState;
        private readonly ISettingsService _settings;
        private readonly INavigationService _navigation;
        private readonly ILogger<LcsAuthService> _logger;
        private readonly SemaphoreSlim _reAuthLock = new(1, 1);

        public LcsAuthService(ILcsHttpClientService http, ILcsSessionService sessionState, ISettingsService settings, INavigationService navigation, ILogger<LcsAuthService> logger)
        {
            _http = http;
            _sessionState = sessionState;
            _settings = settings;
            _navigation = navigation;
            _logger = logger;
        }

        public bool IsLoggedIn => _http.CookieContainer.GetCookies(new Uri(_http.LcsUrl)).Count > 0;

        public void SetCookies(CookieContainer cookies)
        {
            _logger.LogDebug("Setting cookies on HTTP client");
            var cookieHeader = cookies.GetCookieHeader(new Uri(_http.LcsUrl));
            foreach (Cookie cookie in cookies.GetCookies(new Uri(_http.LcsUrl)))
                _http.CookieContainer.Add(new Uri(_http.LcsUrl), cookie);
            _settings.Cookie = cookieHeader.Replace(';', ',');
            _settings.Save();
            _logger.LogInformation("Cookies applied and settings saved. IsLoggedIn={IsLoggedIn}", IsLoggedIn);
            _sessionState.NotifyLoggedIn();
        }

        public async Task<bool> SilentReAuthAsync()
        {
            await _reAuthLock.WaitAsync();
            try
            {
                _logger.LogInformation("Silent re-authentication started via headless WebView2 SSO");

                var ssoCookies = await _navigation.TryHeadlessSsoAsync();
                if (ssoCookies == null)
                {
                    _logger.LogWarning("Silent re-authentication failed — headless SSO did not produce cookies");
                    return false;
                }

                SetCookies(ssoCookies);
                _sessionState.InvalidateToken();
                var token = await _sessionState.ForceRefreshTokenAsync();
                if (token != null)
                {
                    _logger.LogInformation("Silent re-authentication succeeded");
                    return true;
                }

                _logger.LogWarning("Silent re-authentication failed — SSO cookies did not yield a valid token");
                return false;
            }
            finally
            {
                _reAuthLock.Release();
            }
        }

        public bool RestoreSavedCookies()
        {
            if (string.IsNullOrEmpty(_settings.Cookie)) return false;
            try
            {
                _http.CookieContainer.SetCookies(new Uri(_http.LcsUrl), _settings.Cookie);
                var cookies = _http.CookieContainer.GetCookies(new Uri(_http.LcsUrl));

                // Log each restored cookie for diagnostics
                foreach (Cookie c in cookies)
                    _logger.LogDebug("Restored cookie: {Name}, Expires={Expires}, Expired={IsExpired}",
                        c.Name,
                        c.Expires == DateTime.MinValue ? "session" : c.Expires.ToLocalTime().ToString("o"),
                        c.Expired);

                // Check explicit-expiry cookies to detect a definitively stale session
                bool anyExplicit = false;
                bool anyStillValid = false;
                foreach (Cookie c in cookies)
                {
                    if (c.Expires == DateTime.MinValue) continue; // session cookie — can't tell locally
                    anyExplicit = true;
                    if (!c.Expired && c.Expires > DateTime.UtcNow)
                        anyStillValid = true;
                }

                // If every cookie with an explicit expiry is past its date, the session is definitely stale
                if (anyExplicit && !anyStillValid)
                {
                    _logger.LogInformation("All persistent cookies are expired — clearing saved session");
                    foreach (Cookie c in cookies) c.Expired = true;
                    _settings.Cookie = string.Empty;
                    _settings.Save();
                    return false;
                }

                _logger.LogInformation("Restored saved cookies. IsLoggedIn={IsLoggedIn}", IsLoggedIn);
                if (IsLoggedIn)
                    _sessionState.NotifyLoggedIn();
                return IsLoggedIn;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore saved cookies");
                return false;
            }
        }

        public void Logout()
        {
            _logger.LogInformation("Logging out — clearing cookies and saved session");
            foreach (Cookie cookie in _http.CookieContainer.GetCookies(new Uri(_http.LcsUrl)))
                cookie.Expired = true;
            _settings.Cookie = string.Empty;
            _settings.LastProjectId = string.Empty;
            _settings.LastProjectName = string.Empty;
            _settings.LastProjectTypeId = 0;
            _settings.Save();
            _sessionState.NotifyLoggedOut();
        }
    }
}
