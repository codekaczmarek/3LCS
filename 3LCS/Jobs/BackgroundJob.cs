using System.Threading.Tasks;

namespace ThreeLCS.Jobs
{
    /// <summary>
    /// Base class for all encapsulated background operations.
    /// Subclasses declare a human-readable <see cref="Name"/> (shown in BackgroundTasksWindow)
    /// and implement <see cref="ExecuteAsync"/> with the full operation logic.
    /// Dependencies are injected via the subclass constructor; runtime parameters are
    /// also passed in via the constructor so that call sites read naturally:
    /// <code>_runner.RunExclusive(new LivenessCheckJob(_livenessService, rows), LivenessKey);</code>
    /// </summary>
    public abstract class BackgroundJob
    {
        /// <summary>Human-readable task name shown in the BackgroundTasksWindow.</summary>
        public abstract string Name { get; }

        /// <summary>
        /// Performs the background work. Implementations must respect
        /// <see cref="IJobContext.Token"/> for cooperative cancellation.
        /// Any unhandled exception is caught by the runner and recorded as a Faulted status.
        /// </summary>
        public abstract Task ExecuteAsync(IJobContext context);
    }
}
