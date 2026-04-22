using CommunityToolkit.Mvvm.ComponentModel;
using ThreeLCS.Models;

namespace ThreeLCS.ViewModels
{
    /// <summary>
    /// Shared view-model for a single LCS environment row.
    /// The same instance is bound to both the MainWindow grid row and
    /// the RdpLaunchWindow, so any state mutation is reflected in both views.
    /// </summary>
    public partial class EnvironmentViewModel : ObservableObject
    {
        [ObservableProperty]
        private CloudHostedInstance _instance;

        [ObservableProperty]
        private LivenessStatus _liveness = LivenessStatus.Unknown;

        public EnvironmentViewModel(CloudHostedInstance instance) => _instance = instance;
    }
}
