using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class ChoosePackageViewModel : ObservableObject
    {
        private readonly ILcsPackageService _packageService;

        [ObservableProperty] private ObservableCollection<DeployablePackage> _packages = new();
        [ObservableProperty] private DeployablePackage? _selectedPackage;
        [ObservableProperty] private bool _isBusy;

        public CloudHostedInstance? Instance { get; set; }

        public ChoosePackageViewModel(ILcsPackageService packageService)
        {
            _packageService = packageService;
        }

        [RelayCommand]
        private async Task Load()
        {
            if (Instance == null) return;
            IsBusy = true;
            try
            {
                var packages = await Task.Run(() => _packageService.GetPagedDeployablePackageList(Instance));
                Packages = new ObservableCollection<DeployablePackage>(packages);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void Select()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.DialogResult = true; break; }
        }

        [RelayCommand]
        private void Cancel()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.DialogResult = false; break; }
        }
    }
}
