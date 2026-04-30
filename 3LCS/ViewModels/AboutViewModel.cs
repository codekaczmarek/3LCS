using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;
using System.Windows;
using ThreeLCS.Infrastructure;

namespace ThreeLCS.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty] private string _version = string.Empty;
        [ObservableProperty] private string _appName = "3LCS";
        [ObservableProperty] private string _copyright = "© 2026 Dawid Kaczmarek — MIT License";

        public AboutViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";
        }

        [RelayCommand]
        private void OpenGithub() => WebBrowserHelper.OpenUri("https://github.com/codekaczmarek/3LCS");

        [RelayCommand]
        private void Close()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.Close(); break; }
        }
    }
}
