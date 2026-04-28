using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ThreeLCS.Jobs
{
    /// <summary>
    /// Polls every 60 seconds and triggers a refresh when the session is active and idle.
    /// Runs as an exclusive background task — starting a new instance cancels the previous one.
    /// </summary>
    public sealed class AutoRefreshJob : BackgroundJob
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Returns true when a refresh should be triggered (logged in, project selected, not busy).
        /// </summary>
        private readonly Func<bool> _canRefresh;

        /// <summary>The refresh action to invoke on the UI thread.</summary>
        private readonly Func<Task> _refresh;

        public override string Name => "Auto Refresh";

        public AutoRefreshJob(ILogger logger, Func<bool> canRefresh, Func<Task> refresh)
        {
            _logger = logger;
            _canRefresh = canRefresh;
            _refresh = refresh;
        }

        public override async Task ExecuteAsync(IJobContext context)
        {
            try
            {
                while (!context.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), context.Token);
                    if (!_canRefresh()) continue;
                    _logger.LogDebug("Auto-refresh triggered");
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        try { await _refresh(); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Auto-refresh failed"); }
                    });
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
