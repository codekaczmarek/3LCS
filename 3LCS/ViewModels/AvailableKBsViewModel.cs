using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ThreeLCS.Infrastructure;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class AvailableKBsViewModel : ObservableObject
    {
        private readonly ILcsDiagnosticsService _diagService;

        [ObservableProperty] private ObservableCollection<Hotfix> _hotfixes = new();
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _title = "Available KBs";

        public CloudHostedInstance? Instance { get; set; }
        public int HotfixesType { get; set; }

        public AvailableKBsViewModel(ILcsDiagnosticsService diagService)
        {
            _diagService = diagService;
        }

        [RelayCommand]
        private async Task Load()
        {
            if (Instance == null) return;
            IsBusy = true;
            try
            {
                var envId = await Task.Run(() => _diagService.GetDiagEnvironmentId(Instance));
                if (envId == null) return;
                var hotfixes = await Task.Run(() => _diagService.GetAvailableHotfixes(envId, HotfixesType));
                if (hotfixes != null) Hotfixes = new ObservableCollection<Hotfix>(hotfixes);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void OpenUrl(Hotfix? hotfix)
        {
            if (hotfix?.Url != null) WebBrowserHelper.OpenUri(hotfix.Url);
        }
    }
}
