using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Messages;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    /// <summary>
    /// Single authority for LCS session state and anti-forgery token management.
    /// Exposes notify methods called by LcsAuthService and LcsServiceBase.
    /// Receives <see cref="SessionStateChangedMessage"/> and <see cref="TokenAcquiredMessage"/>
    /// via the WeakReferenceMessenger to keep state in sync with external senders.
    /// </summary>
    public sealed class LcsSessionService : ObservableObject,
        ILcsSessionService,
        IRecipient<SessionStateChangedMessage>,
        IRecipient<TokenAcquiredMessage>
    {
        private readonly ILogger<LcsSessionService> _logger;
        private readonly ILcsHttpClientService _http;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        private LcsSessionState _currentState = LcsSessionState.NotLoggedIn;
        private string _statusDescription = "Not logged in";
        private string? _requestVerificationToken;

        public LcsSessionState CurrentState => _currentState;
        public bool IsLoggedIn => _currentState == LcsSessionState.LoggedIn;
        public string StatusDescription => _statusDescription;
        public string? RequestVerificationToken => _requestVerificationToken;

        public LcsSessionService(ILcsHttpClientService http, ILogger<LcsSessionService> logger)
        {
            _http = http;
            _logger = logger;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        // ── Notification methods ──────────────────────────────────────────────

        public void NotifyLoggedIn()
        {
            DispatchUI(() =>
            {
                SetProperty(ref _currentState, LcsSessionState.LoggedIn, nameof(CurrentState));
                SetProperty(ref _statusDescription, "Logged in", nameof(StatusDescription));
                OnPropertyChanged(nameof(IsLoggedIn));
                WeakReferenceMessenger.Default.Send(new SessionStateChangedMessage(LcsSessionState.LoggedIn));
            });
        }

        public void NotifyLoggedOut()
        {
            DispatchUI(() =>
            {
                SetProperty(ref _requestVerificationToken, null, nameof(RequestVerificationToken));
                SetProperty(ref _currentState, LcsSessionState.NotLoggedIn, nameof(CurrentState));
                SetProperty(ref _statusDescription, "Not logged in", nameof(StatusDescription));
                OnPropertyChanged(nameof(IsLoggedIn));
                WeakReferenceMessenger.Default.Send(new SessionStateChangedMessage(LcsSessionState.NotLoggedIn));
            });
        }

        public void NotifySessionExpired()
        {
            DispatchUI(() =>
            {
                SetProperty(ref _requestVerificationToken, null, nameof(RequestVerificationToken));
                SetProperty(ref _currentState, LcsSessionState.SessionExpired, nameof(CurrentState));
                SetProperty(ref _statusDescription, "⚠️ Session expired — please log in again", nameof(StatusDescription));
                OnPropertyChanged(nameof(IsLoggedIn));
                WeakReferenceMessenger.Default.Send(new SessionStateChangedMessage(LcsSessionState.SessionExpired));
            });
        }

        public void InvalidateToken()
        {
            _requestVerificationToken = null;
        }

        // ── Token management ──────────────────────────────────────────────────

        public async Task<string?> GetValidTokenAsync()
        {
            if (_requestVerificationToken != null && _currentState == LcsSessionState.LoggedIn)
                return _requestVerificationToken;

            if (_currentState is LcsSessionState.NotLoggedIn or LcsSessionState.SessionExpired)
                return null;

            return await ForceRefreshTokenAsync();
        }

        public async Task<string?> ForceRefreshTokenAsync()
        {
            if (_currentState is LcsSessionState.NotLoggedIn or LcsSessionState.SessionExpired)
                return null;

            await _tokenLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_requestVerificationToken != null && _currentState == LcsSessionState.LoggedIn)
                    return _requestVerificationToken;

                if (_currentState is LcsSessionState.NotLoggedIn or LcsSessionState.SessionExpired)
                    return null;

                var tokenUrl = $"{_http.LcsUrl}/V2";
                var id = Guid.NewGuid();
                var sw = Stopwatch.StartNew();

                DispatchUI(() =>
                {
                    SetProperty(ref _currentState, LcsSessionState.TokenRefreshing, nameof(CurrentState));
                    SetProperty(ref _statusDescription, "🔄 Refreshing token…", nameof(StatusDescription));
                    WeakReferenceMessenger.Default.Send(new SessionStateChangedMessage(LcsSessionState.TokenRefreshing));
                });

                WeakReferenceMessenger.Default.Send(new ApiCallStartedMessage(id, "↻ TOKEN", tokenUrl, DateTime.Now));
                _logger.LogInformation("Fetching request verification token from {Url}", tokenUrl);

                try
                {
                    var response = await _http.HttpClient.GetAsync(tokenUrl);
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var node = doc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']");

                    if (node == null)
                    {
                        sw.Stop();
                        _logger.LogWarning("Request verification token not found in response from {Url} — session likely expired", tokenUrl);
                        WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, (int)response.StatusCode, sw.ElapsedMilliseconds, "Token not found — session expired"));
                        // Token missing from /V2 means LCS redirected us to the login page — the session is gone
                        NotifySessionExpired();
                        return null;
                    }

                    var token = node.Attributes["value"].Value;
                    sw.Stop();

                    _requestVerificationToken = token;
                    WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, (int)response.StatusCode, sw.ElapsedMilliseconds, null));
                    WeakReferenceMessenger.Default.Send(new TokenAcquiredMessage(token, tokenUrl));

                    DispatchUI(() =>
                    {
                        SetProperty(ref _currentState, LcsSessionState.LoggedIn, nameof(CurrentState));
                        SetProperty(ref _statusDescription, "Logged in", nameof(StatusDescription));
                        OnPropertyChanged(nameof(IsLoggedIn));
                        WeakReferenceMessenger.Default.Send(new SessionStateChangedMessage(LcsSessionState.LoggedIn));
                    });

                    _logger.LogDebug("Token fetched successfully (length={Len})", token.Length);
                    return token;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "Failed to fetch request verification token from {Url}", tokenUrl);
                    WeakReferenceMessenger.Default.Send(new ApiCallCompletedMessage(id, null, sw.ElapsedMilliseconds, ex.Message));
                    return null;
                }
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // ── Message receivers ─────────────────────────────────────────────────

        void IRecipient<SessionStateChangedMessage>.Receive(SessionStateChangedMessage message)
        {
            var description = message.NewState switch
            {
                LcsSessionState.NotLoggedIn => "Not logged in",
                LcsSessionState.LoggedIn => "Logged in",
                LcsSessionState.SessionExpired => "⚠️ Session expired — please log in again",
                LcsSessionState.TokenExpired => "🔄 Anti-forgery token expired — refreshing…",
                LcsSessionState.TokenRefreshing => "🔄 Refreshing token…",
                _ => message.NewState.ToString()
            };

            if (message.Detail is { Length: > 0 })
                description += $" ({message.Detail})";

            _logger.LogInformation("Session state → {State}: {Description}", message.NewState, description);

            DispatchUI(() =>
            {
                if (message.NewState is LcsSessionState.SessionExpired or LcsSessionState.NotLoggedIn)
                    SetProperty(ref _requestVerificationToken, null, nameof(RequestVerificationToken));

                SetProperty(ref _currentState, message.NewState, nameof(CurrentState));
                SetProperty(ref _statusDescription, description, nameof(StatusDescription));
                OnPropertyChanged(nameof(IsLoggedIn));
            });
        }

        void IRecipient<TokenAcquiredMessage>.Receive(TokenAcquiredMessage message)
        {
            _logger.LogDebug("Token acquired from {Url} (length={Len})", message.FetchedFrom, message.Token.Length);
            DispatchUI(() => SetProperty(ref _requestVerificationToken, message.Token, nameof(RequestVerificationToken)));
        }

        private static void DispatchUI(Action action)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
                action();
            else
                Application.Current?.Dispatcher.InvokeAsync(action);
        }
    }
}
