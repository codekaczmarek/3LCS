using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThreeLCS.Messages;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    /// <summary>
    /// Base class for all LCS API service implementations.
    /// Standardises HTTP execution, response unwrapping, JSON deserialisation, logging,
    /// and API call monitoring via the WeakReferenceMessenger.
    /// Token injection is handled per-request via ILcsSessionService.
    /// HTTP 498 (session expired) triggers a transparent single retry after silent re-authentication;
    /// only a second consecutive 498 propagates to the caller.
    /// </summary>
    public abstract class LcsServiceBase
    {
        protected readonly ILcsHttpClientService Http;
        private readonly ILcsSessionService _sessionState;
        private readonly ILcsAuthService _auth;
        private readonly ILogger _logger;

        private static readonly JsonSerializerSettings LenientSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private static readonly JsonSerializerSettings TypedSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        protected LcsServiceBase(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, ILogger logger)
        {
            Http = http;
            _sessionState = sessionState;
            _auth = auth;
            _logger = logger;
        }

        protected static long Ts() => DateTimeOffset.Now.ToUnixTimeSeconds();

        // ── Core execution ─────────────────────────────────────────────────────

        private async Task<string> ExecuteGetAsync(string url)
        {
            var id = Guid.NewGuid();
            var sw = Stopwatch.StartNew();
            WeakReferenceMessenger.Default.Send(new ApiCallStartedMessage(id, "GET", url, DateTime.Now));
            _logger.LogDebug("GET {Url}", url);

            bool sent = false;
            try
            {
                var response = await Http.HttpClient.GetAsync(url);

                if ((int)response.StatusCode == 498)
                {
                    response.Dispose();
                    _logger.LogWarning("GET {Url}: HTTP 498 — attempting silent re-authentication", url);

                    if (await _auth.SilentReAuthAsync())
                    {
                        response = await Http.HttpClient.GetAsync(url);
                        _logger.LogInformation("GET {Url} retry after re-auth → HTTP {StatusCode}", url, (int)response.StatusCode);
                    }
                    else
                    {
                        sw.Stop();
                        sent = true;
                        WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, 498, sw.ElapsedMilliseconds, "Session expired — re-auth failed"));
                        _sessionState.NotifySessionExpired();
                        throw new HttpRequestException($"GET {url} failed: HTTP 498 and silent re-authentication was unsuccessful.");
                    }
                }

                sw.Stop();
                sent = true;
                _logger.LogDebug("GET {Url} → HTTP {StatusCode}", url, (int)response.StatusCode);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, (int)response.StatusCode, sw.ElapsedMilliseconds, null));
                if ((int)response.StatusCode == 498)
                    _sessionState.NotifySessionExpired();
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) when (!sent)
            {
                sw.Stop();
                _logger.LogError(ex, "GET {Url} failed", url);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, null, sw.ElapsedMilliseconds, ex.Message));
                throw;
            }
        }

        private string ExecuteGetSync(string url)
        {
            var id = Guid.NewGuid();
            var sw = Stopwatch.StartNew();
            WeakReferenceMessenger.Default.Send(new ApiCallStartedMessage(id, "GET", url, DateTime.Now));
            _logger.LogDebug("GET {Url}", url);

            bool sent = false;
            try
            {
                var response = Http.HttpClient.GetAsync(url).GetAwaiter().GetResult();

                if ((int)response.StatusCode == 498)
                {
                    response.Dispose();
                    _logger.LogWarning("GET {Url}: HTTP 498 — attempting silent re-authentication", url);

                    if (_auth.SilentReAuthAsync().GetAwaiter().GetResult())
                    {
                        response = Http.HttpClient.GetAsync(url).GetAwaiter().GetResult();
                        _logger.LogInformation("GET {Url} retry after re-auth → HTTP {StatusCode}", url, (int)response.StatusCode);
                    }
                    else
                    {
                        sw.Stop();
                        sent = true;
                        WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, 498, sw.ElapsedMilliseconds, "Session expired — re-auth failed"));
                        _sessionState.NotifySessionExpired();
                        throw new HttpRequestException($"GET {url} failed: HTTP 498 and silent re-authentication was unsuccessful.");
                    }
                }

                sw.Stop();
                sent = true;
                _logger.LogDebug("GET {Url} → HTTP {StatusCode}", url, (int)response.StatusCode);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, (int)response.StatusCode, sw.ElapsedMilliseconds, null));
                if ((int)response.StatusCode == 498)
                    _sessionState.NotifySessionExpired();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) when (!sent)
            {
                sw.Stop();
                _logger.LogError(ex, "GET {Url} failed", url);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, null, sw.ElapsedMilliseconds, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Executes a POST with per-request token injection and content factory for retry.
        /// Retries once on HTTP 403 after invalidating and force-refreshing the anti-forgery token.
        /// Retries once on HTTP 498 after silent re-authentication (cookie refresh + token refresh).
        /// </summary>
        private async Task<string> ExecutePostAsync(string url, Func<HttpContent> contentFactory)
        {
            var id = Guid.NewGuid();
            var sw = Stopwatch.StartNew();
            WeakReferenceMessenger.Default.Send(new ApiCallStartedMessage(id, "POST", url, DateTime.Now));
            _logger.LogDebug("POST {Url}", url);

            bool sent = false;
            try
            {
                var token = await _sessionState.GetValidTokenAsync();

                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentFactory() };
                if (token != null)
                    request.Headers.TryAddWithoutValidation("__RequestVerificationToken", token);
                HttpResponseMessage response = await Http.HttpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("POST {Url}: HTTP 403 — invalidating token and retrying", url);
                    response.Dispose();
                    _sessionState.InvalidateToken();
                    var newToken = await _sessionState.ForceRefreshTokenAsync();
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentFactory() };
                    if (newToken != null)
                        retryRequest.Headers.TryAddWithoutValidation("__RequestVerificationToken", newToken);
                    response = await Http.HttpClient.SendAsync(retryRequest);
                }

                if ((int)response.StatusCode == 498)
                {
                    response.Dispose();
                    _logger.LogWarning("POST {Url}: HTTP 498 — attempting silent re-authentication", url);

                    if (await _auth.SilentReAuthAsync())
                    {
                        var freshToken = await _sessionState.GetValidTokenAsync();
                        using var reAuthRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentFactory() };
                        if (freshToken != null)
                            reAuthRequest.Headers.TryAddWithoutValidation("__RequestVerificationToken", freshToken);
                        response = await Http.HttpClient.SendAsync(reAuthRequest);
                        _logger.LogInformation("POST {Url} retry after re-auth → HTTP {StatusCode}", url, (int)response.StatusCode);
                    }
                    else
                    {
                        sw.Stop();
                        sent = true;
                        WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, 498, sw.ElapsedMilliseconds, "Session expired — re-auth failed"));
                        _sessionState.NotifySessionExpired();
                        throw new HttpRequestException($"POST {url} failed: HTTP 498 and silent re-authentication was unsuccessful.");
                    }
                }

                sw.Stop();
                sent = true;
                _logger.LogDebug("POST {Url} → HTTP {StatusCode}", url, (int)response.StatusCode);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, (int)response.StatusCode, sw.ElapsedMilliseconds, null));
                if ((int)response.StatusCode == 498)
                    _sessionState.NotifySessionExpired();
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) when (!sent)
            {
                sw.Stop();
                _logger.LogError(ex, "POST {Url} failed", url);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, null, sw.ElapsedMilliseconds, ex.Message));
                throw;
            }
        }

        private string ExecutePostSync(string url, Func<HttpContent> contentFactory)
        {
            var id = Guid.NewGuid();
            var sw = Stopwatch.StartNew();
            WeakReferenceMessenger.Default.Send(new ApiCallStartedMessage(id, "POST", url, DateTime.Now));
            _logger.LogDebug("POST {Url}", url);

            bool sent = false;
            try
            {
                var token = _sessionState.GetValidTokenAsync().GetAwaiter().GetResult();

                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentFactory() };
                if (token != null)
                    request.Headers.TryAddWithoutValidation("__RequestVerificationToken", token);
                HttpResponseMessage response = Http.HttpClient.SendAsync(request).GetAwaiter().GetResult();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("POST {Url}: HTTP 403 — invalidating token and retrying", url);
                    response.Dispose();
                    _sessionState.InvalidateToken();
                    var newToken = _sessionState.ForceRefreshTokenAsync().GetAwaiter().GetResult();
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentFactory() };
                    if (newToken != null)
                        retryRequest.Headers.TryAddWithoutValidation("__RequestVerificationToken", newToken);
                    response = Http.HttpClient.SendAsync(retryRequest).GetAwaiter().GetResult();
                }

                if ((int)response.StatusCode == 498)
                {
                    response.Dispose();
                    _logger.LogWarning("POST {Url}: HTTP 498 — attempting silent re-authentication", url);

                    if (_auth.SilentReAuthAsync().GetAwaiter().GetResult())
                    {
                        var freshToken = _sessionState.GetValidTokenAsync().GetAwaiter().GetResult();
                        using var reAuthRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentFactory() };
                        if (freshToken != null)
                            reAuthRequest.Headers.TryAddWithoutValidation("__RequestVerificationToken", freshToken);
                        response = Http.HttpClient.SendAsync(reAuthRequest).GetAwaiter().GetResult();
                        _logger.LogInformation("POST {Url} retry after re-auth → HTTP {StatusCode}", url, (int)response.StatusCode);
                    }
                    else
                    {
                        sw.Stop();
                        sent = true;
                        WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, 498, sw.ElapsedMilliseconds, "Session expired — re-auth failed"));
                        _sessionState.NotifySessionExpired();
                        throw new HttpRequestException($"POST {url} failed: HTTP 498 and silent re-authentication was unsuccessful.");
                    }
                }

                sw.Stop();
                sent = true;
                _logger.LogDebug("POST {Url} → HTTP {StatusCode}", url, (int)response.StatusCode);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, (int)response.StatusCode, sw.ElapsedMilliseconds, null));
                if ((int)response.StatusCode == 498)
                    _sessionState.NotifySessionExpired();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) when (!sent)
            {
                sw.Stop();
                _logger.LogError(ex, "POST {Url} failed", url);
                WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, null, sw.ElapsedMilliseconds, ex.Message));
                throw;
            }
        }

        // ── Response parsing ───────────────────────────────────────────────────

        private Response? ParseResponse(string body)
        {
            var response = JsonConvert.DeserializeObject<Response>(body);
            if (response?.Success == false)
                _logger.LogWarning("LCS API returned Success=false. Message: {Message}", response.Message);
            return response;
        }

        private T? ExtractData<T>(Response? response) where T : class
        {
            if (response?.Success != true || response.Data == null) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(response.Data.ToString()!, LenientSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialise Response.Data as {Type}", typeof(T).Name);
                return null;
            }
        }

        private T? DeserializeDirect<T>(string body) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(body, LenientSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialise response body as {Type}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// Strips a surrounding JSONP wrapper — e.g. <c>({...})</c> → <c>{...}</c> — then deserialises.
        /// Used by LCS endpoints that return JSONP-style responses.
        /// </summary>
        private T? DeserializeJsonp<T>(string body) where T : class
            => DeserializeDirect<T>(body.TrimStart('(').TrimEnd(')'));

        // ── Async helpers ──────────────────────────────────────────────────────

        protected async Task<Response?> GetResponseAsync(string url)
            => ParseResponse(await ExecuteGetAsync(url));

        protected async Task<T?> GetAsync<T>(string url) where T : class
            => ExtractData<T>(await GetResponseAsync(url));

        protected async Task<T?> GetDirectAsync<T>(string url) where T : class
            => DeserializeDirect<T>(await ExecuteGetAsync(url));

        protected async Task<T?> GetDirectJsonpAsync<T>(string url) where T : class
            => DeserializeJsonp<T>(await ExecuteGetAsync(url));

        protected async Task<Response?> PostFormResponseAsync(string url, string formData)
            => ParseResponse(await ExecutePostAsync(url,
                () => new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded")));

        protected async Task<string?> PostFormRawAsync(string url, string formData)
        {
            try
            {
                return await ExecutePostAsync(url,
                    () => new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostFormRaw failed for {Url}", url);
                return null;
            }
        }

        protected async Task<T?> PostFormAsync<T>(string url, string formData) where T : class
            => ExtractData<T>(await PostFormResponseAsync(url, formData));

        protected async Task<Response?> PostJsonResponseAsync(string url, object payload)
        {
            var json = JsonConvert.SerializeObject(payload, TypedSerializerSettings);
            return ParseResponse(await ExecutePostAsync(url,
                () => new StringContent(json, Encoding.UTF8, "application/json")));
        }

        protected async Task<T?> PostJsonAsync<T>(string url, object payload) where T : class
            => ExtractData<T>(await PostJsonResponseAsync(url, payload));

        // ── Sync helpers ───────────────────────────────────────────────────────

        protected Response? GetResponseSync(string url)
            => ParseResponse(ExecuteGetSync(url));

        protected T? GetSync<T>(string url) where T : class
            => ExtractData<T>(GetResponseSync(url));

        protected T? GetDirectSync<T>(string url) where T : class
            => DeserializeDirect<T>(ExecuteGetSync(url));

        protected T? GetDirectJsonpSync<T>(string url) where T : class
            => DeserializeJsonp<T>(ExecuteGetSync(url));

        protected Response? PostFormResponseSync(string url, string formData)
            => ParseResponse(ExecutePostSync(url,
                () => new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded")));

        protected T? PostFormSync<T>(string url, string formData) where T : class
            => ExtractData<T>(PostFormResponseSync(url, formData));

        protected Response? PostJsonResponseSync(string url, object payload)
        {
            var json = JsonConvert.SerializeObject(payload, TypedSerializerSettings);
            return ParseResponse(ExecutePostSync(url,
                () => new StringContent(json, Encoding.UTF8, "application/json")));
        }

        protected T? PostJsonSync<T>(string url, object payload) where T : class
            => ExtractData<T>(PostJsonResponseSync(url, payload));
    }
}