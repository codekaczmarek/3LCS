using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class BuildInfoDetailsViewModel : ObservableObject
    {
        private readonly ILcsDiagnosticsService _diagService;
        private readonly IDialogService _dialog;

        [ObservableProperty] private BuildInfoDetails? _buildInfo;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = string.Empty;

        public CloudHostedInstance? Instance { get; set; }

        public BuildInfoDetailsViewModel(ILcsDiagnosticsService diagService, IDialogService dialog)
        {
            _diagService = diagService;
            _dialog = dialog;
        }

        [RelayCommand]
        private async Task Load()
        {
            if (Instance == null) return;
            IsBusy = true;
            StatusText = "Loading build info…";
            try
            {
                var envId = await Task.Run(() => _diagService.GetBuildInfoEnvironmentId(Instance));
                if (envId == 0)
                {
                    StatusText = "Build info diagnostics not available for this environment.";
                    return;
                }
                BuildInfo = await Task.Run(() => _diagService.GetEnvironmentBuildInfoDetails(Instance, envId.ToString()));
                StatusText = BuildInfo?.BuildInfoTreeView?.Count > 0
                    ? $"Loaded {BuildInfo.BuildInfoTreeView.Count} entries."
                    : "No build info data returned.";
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to load build info: {ex.Message}";
                _dialog.ShowError($"Build info could not be loaded.\n\n{ex.Message}");
            }
            finally { IsBusy = false; }
        }
    }
}
