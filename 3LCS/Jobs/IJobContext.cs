using System.Threading;

namespace ThreeLCS.Jobs
{
    /// <summary>
    /// Execution context passed to every <see cref="BackgroundJob.ExecuteAsync"/> call.
    /// Provides the cancellation token managed by the background task infrastructure and
    /// a channel for reporting progress messages visible in the BackgroundTasksWindow.
    /// </summary>
    public interface IJobContext
    {
        /// <summary>Token that is cancelled when the task is cancelled from the UI or on logout.</summary>
        CancellationToken Token { get; }

        /// <summary>Updates the status message shown for this task in the BackgroundTasksWindow.</summary>
        void Report(string message);
    }
}
