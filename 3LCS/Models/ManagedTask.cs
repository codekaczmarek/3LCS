using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;

namespace ThreeLCS.Models
{
    public partial class ManagedTask : ObservableObject
    {
        private readonly CancellationTokenSource _cts = new();

        public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
        public string Name { get; }
        public DateTime StartedAt { get; } = DateTime.Now;

        [ObservableProperty] private ManagedTaskStatus _status = ManagedTaskStatus.Running;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public CancellationToken Token => _cts.Token;

        public ManagedTask(string name) => Name = name;

        public void Cancel()
        {
            if (Status != ManagedTaskStatus.Running) return;
            _cts.Cancel();
            Status = ManagedTaskStatus.Cancelled;
        }

        internal void Complete()
        {
            if (Status == ManagedTaskStatus.Running)
                Status = ManagedTaskStatus.Completed;
        }

        internal void Fail(string message)
        {
            StatusMessage = message;
            if (Status == ManagedTaskStatus.Running)
                Status = ManagedTaskStatus.Faulted;
        }

        public void UpdateMessage(string message) => StatusMessage = message;
    }
}
