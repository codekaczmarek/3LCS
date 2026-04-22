using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ThreeLCS.ViewModels
{
    public partial class LogDisplayViewModel : ObservableObject
    {
        [ObservableProperty] private string _logText = string.Empty;

        [RelayCommand]
        private void Close()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.Close(); break; }
        }

        [RelayCommand]
        private void Copy() => Clipboard.SetText(LogText);
    }
}
