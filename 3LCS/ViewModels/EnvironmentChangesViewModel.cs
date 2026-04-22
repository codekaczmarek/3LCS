using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class EnvironmentChangesViewModel : ObservableObject
    {
        private readonly ILcsEnvironmentService _envService;

        [ObservableProperty] private ObservableCollection<ActionDetails> _actions = new();
        [ObservableProperty] private bool _isBusy;

        public CloudHostedInstance? Instance { get; set; }

        public EnvironmentChangesViewModel(ILcsEnvironmentService envService)
        {
            _envService = envService;
        }

        [RelayCommand]
        private async Task Load()
        {
            if (Instance == null) return;
            IsBusy = true;
            try
            {
                var actions = await _envService.GetEnvironmentHistoryDetailsAsync(Instance);
                Actions = new ObservableCollection<ActionDetails>(actions);
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
