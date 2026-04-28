using System.Threading.Tasks;
using ThreeLCS.Jobs;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    /// <summary>
    /// Unified entry point for executing <see cref="BackgroundJob"/> instances.
    /// Three execution shapes cover every call pattern in the application:
    /// <list type="bullet">
    ///   <item><see cref="Run"/> — fire-and-forget; task visible in BackgroundTasksWindow.</item>
    ///   <item><see cref="RunExclusive"/> — single-instance fire-and-forget; automatically cancels
    ///     any previously running job with the same key before starting the new one.</item>
    ///   <item><see cref="RunAsCommandAsync"/> — awaitable; handles IsBusy + error dialog automatically.
    ///     Intended for short command operations that need to block the UI while running.</item>
    /// </list>
    /// </summary>
    public interface IBackgroundJobRunner
    {
        /// <summary>
        /// Fire-and-forget execution. The task appears in BackgroundTasksWindow and is
        /// auto-removed ~4 seconds after completion.
        /// </summary>
        ManagedTask Run(BackgroundJob job);

        /// <summary>
        /// Single-instance fire-and-forget. If a job with the same <paramref name="key"/>
        /// is currently running it is cancelled before the new one starts.
        /// If <paramref name="key"/> is null, the job's full type name is used as the key.
        /// </summary>
        ManagedTask RunExclusive(BackgroundJob job, string? key = null);

        /// <summary>Returns the active exclusive task for the given key, or null.</summary>
        ManagedTask? GetExclusive(string key);

        /// <summary>Cancels and removes the exclusive task for the given key, if running.</summary>
        void CancelExclusive(string key);

        /// <summary>
        /// Awaitable execution with automatic IsBusy management and error-dialog handling.
        /// Does NOT appear in BackgroundTasksWindow — intended for short command operations
        /// that already have the status-bar busy spinner as visual feedback.
        /// Returns <c>true</c> if the job completed successfully, <c>false</c> if it threw
        /// (the error dialog will already have been shown to the user).
        /// </summary>
        Task<bool> RunAsCommandAsync(BackgroundJob job, IBusyHost host, IDialogService dialog);
    }
}
