using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface IBackgroundTaskService
    {
        ObservableCollection<ManagedTask> Tasks { get; }

        /// <summary>
        /// Starts a named background task. The work delegate receives a cancellation token.
        /// Call <see cref="ManagedTask.Cancel"/> on the returned object to cancel it.
        /// The task is automatically removed from <see cref="Tasks"/> a few seconds after it finishes.
        /// </summary>
        ManagedTask Run(string name, Func<CancellationToken, Task> work);
    }
}
