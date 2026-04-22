using CommunityToolkit.Mvvm.ComponentModel;

namespace ThreeLCS.Models
{
    /// <summary>
    /// Thin wrapper that pairs a <see cref="CloudHostedInstance"/> with its
    /// asynchronously-resolved <see cref="LivenessStatus"/>.
    /// </summary>
    public partial class EnvironmentRow : ObservableObject
    {
        [ObservableProperty]
        private CloudHostedInstance _instance;

        [ObservableProperty]
        private LivenessStatus _liveness = LivenessStatus.Unknown;

        public EnvironmentRow(CloudHostedInstance instance) => _instance = instance;
    }
}
