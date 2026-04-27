using CommunityToolkit.Mvvm.ComponentModel;
using ThreeLCS.Models;

namespace ThreeLCS.ViewModels
{
    /// <summary>
    /// Shared view-model for a single LCS environment row.
    /// The same instance is bound to both the MainWindow grid row and
    /// the RdpLaunchWindow, so any state mutation is reflected in both views.
    /// When used in the Favourites tile view, <see cref="ProjectId"/> and
    /// <see cref="ProjectName"/> are populated; they are null for the CHE/SaaS grids.
    /// </summary>
    public partial class EnvironmentViewModel : ObservableObject
    {
        [ObservableProperty]
        private CloudHostedInstance _instance;

        [ObservableProperty]
        private LivenessStatus _liveness = LivenessStatus.Unknown;

        /// <summary>LCS project ID — set only for favourite environment tiles.</summary>
        [ObservableProperty]
        private int? _projectId;

        /// <summary>LCS project display name — set only for favourite environment tiles.</summary>
        [ObservableProperty]
        private string? _projectName;

        /// <summary>LCS project type — set only for favourite environment tiles.</summary>
        [ObservableProperty]
        private int? _projectTypeId;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayPrimary), nameof(DisplaySecondary), nameof(HasFriendlyName))]
        private string? _friendlyName;

        public bool HasFriendlyName => !string.IsNullOrEmpty(FriendlyName);
        public string DisplayPrimary => HasFriendlyName ? FriendlyName! : (Instance.DisplayName ?? string.Empty);
        public string? DisplaySecondary => HasFriendlyName ? Instance.DisplayName : null;

        public EnvironmentViewModel(CloudHostedInstance instance) => _instance = instance;

        partial void OnInstanceChanged(CloudHostedInstance value)
        {
            OnPropertyChanged(nameof(DisplayPrimary));
            OnPropertyChanged(nameof(DisplaySecondary));
        }
    }
}
