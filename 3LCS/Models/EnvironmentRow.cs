using CommunityToolkit.Mvvm.ComponentModel;

namespace ThreeLCS.Models
{
    /// <summary>
    /// Thin wrapper that pairs a <see cref="CloudHostedInstance"/> with its
    /// asynchronously-resolved <see cref="LivenessStatus"/>.
    /// </summary>
    public partial class EnvironmentRow : ObservableObject
    {
        public CloudHostedInstance Instance { get; }

        [ObservableProperty]
        private LivenessStatus _liveness = LivenessStatus.Unknown;

        public EnvironmentRow(CloudHostedInstance instance) => Instance = instance;
    }
}
