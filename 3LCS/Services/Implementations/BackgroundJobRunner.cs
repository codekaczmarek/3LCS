using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThreeLCS.Jobs;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public sealed class BackgroundJobRunner : IBackgroundJobRunner
    {
        private readonly IBackgroundTaskService _taskService;
        private readonly Dictionary<string, ManagedTask> _exclusiveSlots = new();
        private readonly object _lock = new();

        public BackgroundJobRunner(IBackgroundTaskService taskService)
        {
            _taskService = taskService;
        }

        public ManagedTask Run(BackgroundJob job)
            => _taskService.Run(job.Name, ct => job.ExecuteAsync(new JobContext(ct)));

        public ManagedTask RunExclusive(BackgroundJob job, string? key = null)
        {
            var slotKey = key ?? job.GetType().FullName!;
            lock (_lock)
            {
                if (_exclusiveSlots.TryGetValue(slotKey, out var existing) &&
                    existing.Status == ManagedTaskStatus.Running)
                    existing.Cancel();

                var task = _taskService.Run(job.Name, ct => job.ExecuteAsync(new JobContext(ct)));
                _exclusiveSlots[slotKey] = task;
                return task;
            }
        }

        public ManagedTask? GetExclusive(string key)
        {
            lock (_lock)
            {
                _exclusiveSlots.TryGetValue(key, out var task);
                return task;
            }
        }

        public void CancelExclusive(string key)
        {
            lock (_lock)
            {
                if (_exclusiveSlots.TryGetValue(key, out var task))
                {
                    task.Cancel();
                    _exclusiveSlots.Remove(key);
                }
            }
        }

        public async Task<bool> RunAsCommandAsync(BackgroundJob job, IBusyHost host, IDialogService dialog)
        {
            host.IsBusy = true;
            host.StatusText = $"{job.Name}\u2026";
            try
            {
                using var cts = new CancellationTokenSource();
                await job.ExecuteAsync(new JobContext(cts.Token, msg => host.StatusText = msg));
                return true;
            }
            catch (Exception ex)
            {
                dialog.ShowError(ex.Message);
                return false;
            }
            finally
            {
                host.IsBusy = false;
                host.StatusText = "Ready";
            }
        }

        // ── Private implementation ────────────────────────────────────────────

        private sealed class JobContext : IJobContext
        {
            private readonly Action<string>? _report;

            public JobContext(CancellationToken token, Action<string>? report = null)
            {
                Token = token;
                _report = report;
            }

            public CancellationToken Token { get; }
            public void Report(string message) => _report?.Invoke(message);
        }
    }
}
