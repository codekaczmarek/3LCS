using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace ThreeLCS.ViewModels
{
    /// <summary>
    /// UI projection of a <see cref="ThreeLCS.Models.FavouriteProject"/> entry.
    /// Knows nothing about persistence — expand-state changes are relayed back to the
    /// caller via a callback so that only the service layer writes to disk.
    /// </summary>
    public partial class FavouriteProjectViewModel : ObservableObject
    {
        private readonly Action<bool> _onExpandedChanged;

        public int ProjectId { get; }
        public string ProjectName { get; }
        public int ProjectTypeId { get; }

        /// <summary>True when the user explicitly starred this project (persisted flag).</summary>
        public bool IsExplicitFavourite { get; }

        public ObservableCollection<EnvironmentViewModel> Environments { get; } = new();

        public bool HasEnvironments => Environments.Count > 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayPrimary), nameof(DisplaySecondary), nameof(HasFriendlyName))]
        private string? _friendlyName;

        public bool HasFriendlyName => !string.IsNullOrEmpty(FriendlyName);
        public string DisplayPrimary => HasFriendlyName ? FriendlyName! : ProjectName;
        public string? DisplaySecondary => HasFriendlyName ? ProjectName : null;

        [ObservableProperty]
        private bool _isExpanded;

        partial void OnIsExpandedChanged(bool value) => _onExpandedChanged(value);

        public FavouriteProjectViewModel(
            int projectId,
            string projectName,
            int projectTypeId,
            bool isExplicitFavourite,
            bool isExpanded,
            string? friendlyName,
            Action<bool> onExpandedChanged)
        {
            ProjectId = projectId;
            ProjectName = projectName;
            ProjectTypeId = projectTypeId;
            IsExplicitFavourite = isExplicitFavourite;
            _isExpanded = isExpanded;
            _friendlyName = friendlyName;
            _onExpandedChanged = onExpandedChanged;

            Environments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasEnvironments));
        }
    }
}
