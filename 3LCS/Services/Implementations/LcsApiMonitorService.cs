using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ThreeLCS.Messages;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    /// <summary>
    /// Receives API call lifecycle messages (via WeakReferenceMessenger) and maintains
    /// an observable log of recent calls for display in the main window.
    /// </summary>
    public partial class LcsApiMonitorService : ObservableObject, ILcsApiMonitorService,
        IRecipient<ApiCallStartedMessage>, IRecipient<ApiCallCompletedMessage>,
        IRecipient<SessionStateChangedMessage>
    {
        private const int MaxEntries = 100;

        private readonly ObservableCollection<ApiMonitorEntry> _calls = new();
        private readonly ConcurrentDictionary<Guid, ApiMonitorEntry> _active = new();

        [ObservableProperty] private string _currentOperation = "Idle";
        [ObservableProperty] private int _activeCallCount;

        public ReadOnlyObservableCollection<ApiMonitorEntry> RecentCalls { get; }

        public LcsApiMonitorService()
        {
            RecentCalls = new ReadOnlyObservableCollection<ApiMonitorEntry>(_calls);
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(ApiCallStartedMessage msg)
        {
            var entry = new ApiMonitorEntry
            {
                Id = msg.Id,
                Method = msg.Method,
                Url = msg.Url,
                StartedAt = msg.StartedAt
            };

            _active[msg.Id] = entry;

            DispatchUI(() =>
            {
                _calls.Insert(0, entry);
                while (_calls.Count > MaxEntries)
                    _calls.RemoveAt(_calls.Count - 1);

                ActiveCallCount = _active.Count;
                CurrentOperation = $"{msg.Method} {ShortPath(msg.Url)}";
            });
        }

        public void Receive(ApiCallCompletedMessage msg)
        {
            if (!_active.TryRemove(msg.Id, out var entry)) return;

            DispatchUI(() =>
            {
                entry.Status = msg.Error != null
                    ? $"Error: {msg.Error}"
                    : $"HTTP {msg.StatusCode}";
                entry.DurationMs = msg.DurationMs;

                ActiveCallCount = _active.Count;

                // Show the most recently started call that is still in flight, or "Idle"
                var newest = _active.Values.OrderByDescending(e => e.StartedAt).FirstOrDefault();
                CurrentOperation = newest != null
                    ? $"{newest.Method} {ShortPath(newest.Url)}"
                    : "Idle";
            });
        }

        private static void DispatchUI(Action action)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
                action();
            else
                Application.Current?.Dispatcher.InvokeAsync(action);
        }

        private static string ShortPath(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;

        public void Receive(SessionStateChangedMessage msg)
        {
            var text = msg.NewState switch
            {
                LcsSessionState.SessionExpired => "⚠️ Session expired",
                LcsSessionState.NotLoggedIn => "Not logged in",
                LcsSessionState.TokenExpired => "🔄 Token expired",
                LcsSessionState.TokenRefreshing => "🔄 Refreshing token…",
                _ => null
            };
            if (text != null)
                DispatchUI(() => CurrentOperation = text);
        }
    }
}
