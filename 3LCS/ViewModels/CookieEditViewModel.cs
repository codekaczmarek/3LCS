using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class CookieEditViewModel : ObservableObject
    {
        private readonly ISettingsService _settings;

        [ObservableProperty] private string _cookieText = string.Empty;

        public CookieEditViewModel(ISettingsService settings)
        {
            _settings = settings;
            CookieText = settings.Cookie;
        }

        [RelayCommand]
        private void Save()
        {
            _settings.Cookie = CookieText;
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
