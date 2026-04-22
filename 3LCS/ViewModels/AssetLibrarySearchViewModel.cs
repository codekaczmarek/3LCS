using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class AssetLibrarySearchViewModel : ObservableObject
    {
        private readonly ILcsAssetLibraryService _assetLibraryService;

        [ObservableProperty] private string _searchTerm = string.Empty;
        [ObservableProperty] private ObservableCollection<Asset> _results = new();
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private AssetFileType _selectedAssetType = AssetFileType.SoftwareDeployablePackage;

        public AssetLibrarySearchViewModel(ILcsAssetLibraryService assetLibraryService)
        {
            _assetLibraryService = assetLibraryService;
        }

        [RelayCommand]
        private async Task Search()
        {
            IsBusy = true;
            Results.Clear();
            try
            {
                var assets = await Task.Run(() => _assetLibraryService.GetSharedAssetList(SelectedAssetType));
                foreach (var asset in assets)
                    if (string.IsNullOrEmpty(SearchTerm) || (asset.Name?.Contains(SearchTerm, System.StringComparison.OrdinalIgnoreCase) == true))
                        Results.Add(asset);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void Close()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.Close(); break; }
        }
    }
}
