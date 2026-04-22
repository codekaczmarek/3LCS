using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ThreeLCS.Infrastructure;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class ParametersViewModel : ObservableObject
    {
        private readonly ISettingsService _settings;

        [ObservableProperty] private bool _cachingEnabled;
        [ObservableProperty] private bool _keepCache;
        [ObservableProperty] private bool _alwaysLogAsAdmin;
        [ObservableProperty] private bool _autoRefresh;
        [ObservableProperty] private bool _livenessCheckEnabled;

        [ObservableProperty] private string _lcsUrl = string.Empty;
        [ObservableProperty] private string _lcsUpdateUrl = string.Empty;
        [ObservableProperty] private string _lcsDiagUrl = string.Empty;
        [ObservableProperty] private string _lcsFixUrl = string.Empty;
        [ObservableProperty] private bool _urlsAreCustom;

        public IReadOnlyList<GeoPreset> GeoPresets => Infrastructure.GeoPresets.All;

        private GeoPreset? _selectedGeo;
        public GeoPreset? SelectedGeo
        {
            get => _selectedGeo;
            set
            {
                if (SetProperty(ref _selectedGeo, value) && value != null && value.Name != "Custom")
                {
                    LcsUrl = value.LcsUrl;
                    LcsUpdateUrl = value.LcsUpdateUrl;
                    LcsDiagUrl = value.LcsDiagUrl;
                    LcsFixUrl = value.LcsFixUrl;
                    UrlsAreCustom = false;
                }
                else if (value?.Name == "Custom")
                {
                    UrlsAreCustom = true;
                }
            }
        }

        public ParametersViewModel(ISettingsService settings)
        {
            _settings = settings;
            CachingEnabled = settings.CachingEnabled;
            KeepCache = settings.KeepCache;
            AlwaysLogAsAdmin = settings.AlwaysLogAsAdmin;
            AutoRefresh = settings.AutoRefresh;
            LivenessCheckEnabled = settings.LivenessCheckEnabled;

            LcsUrl = settings.LcsUrl;
            LcsUpdateUrl = settings.LcsUpdateUrl;
            LcsDiagUrl = settings.LcsDiagUrl;
            LcsFixUrl = settings.LcsFixUrl;

            // Match saved URLs to a preset (or Custom)
            var match = GeoPresets.FirstOrDefault(g =>
                g.Name != "Custom" &&
                g.LcsUrl == LcsUrl && g.LcsUpdateUrl == LcsUpdateUrl &&
                g.LcsDiagUrl == LcsDiagUrl && g.LcsFixUrl == LcsFixUrl);
            _selectedGeo = match ?? GeoPresets.FirstOrDefault(g => g.Name == "Custom");
            UrlsAreCustom = _selectedGeo?.Name == "Custom";
        }

        [RelayCommand]
        private void Save()
        {
            bool urlsChanged =
                _settings.LcsUrl != LcsUrl ||
                _settings.LcsUpdateUrl != LcsUpdateUrl ||
                _settings.LcsDiagUrl != LcsDiagUrl ||
                _settings.LcsFixUrl != LcsFixUrl;

            _settings.CachingEnabled = CachingEnabled;
            _settings.KeepCache = KeepCache;
            _settings.AlwaysLogAsAdmin = AlwaysLogAsAdmin;
            _settings.LivenessCheckEnabled = LivenessCheckEnabled;
            _settings.LcsUrl = LcsUrl;
            _settings.LcsUpdateUrl = LcsUpdateUrl;
            _settings.LcsDiagUrl = LcsDiagUrl;
            _settings.LcsFixUrl = LcsFixUrl;

            if (urlsChanged)
            {
                _settings.ClearSession();
                _settings.Save();
                MessageBox.Show(
                    "GEO/URL settings changed. The application will restart to apply the new settings.",
                    "Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null)
                    Process.Start(exePath);
                Application.Current.Shutdown();
                return;
            }

            _settings.Save();
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
