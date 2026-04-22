using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class ChooseServiceViewModel : ObservableObject
    {
        private readonly ILcsServiceRestartService _serviceRestartService;

        [ObservableProperty] private ObservableCollection<ServiceToRestart> _services = new();
        [ObservableProperty] private ServiceToRestart? _selectedService;

        public ChooseServiceViewModel(ILcsServiceRestartService serviceRestartService)
        {
            _serviceRestartService = serviceRestartService;
        }

        public async Task LoadAsync()
        {
            var items = await Task.Run(() => _serviceRestartService.GetServicesToRestart());
            if (items != null) Services = new ObservableCollection<ServiceToRestart>(items);
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
