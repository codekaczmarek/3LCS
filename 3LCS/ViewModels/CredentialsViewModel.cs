using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class CredentialsViewModel : ObservableObject
    {
        private readonly ILcsCredentialsService _credentialsService;

        [ObservableProperty] private Dictionary<string, string> _credentials = new();
        [ObservableProperty] private string _environmentName = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public CloudHostedInstance? Instance { get; set; }

        public CredentialsViewModel(ILcsCredentialsService credentialsService)
        {
            _credentialsService = credentialsService;
        }

        [RelayCommand]
        private async Task Load()
        {
            if (Instance?.Instances == null || Instance.Instances.Length == 0) return;
            IsBusy = true;
            EnvironmentName = Instance.DisplayName ?? string.Empty;
            try
            {
                var creds = await Task.Run(() =>
                    _credentialsService.GetCredentials(Instance.EnvironmentId!, Instance.Instances[0].ItemName!));
                Credentials = creds ?? new Dictionary<string, string>();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void CopyPassword(string? key)
        {
            if (key != null && Credentials.TryGetValue(key, out var password))
                Clipboard.SetText(password);
        }

        [RelayCommand]
        private void Close()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.Close(); break; }
        }
    }
}
