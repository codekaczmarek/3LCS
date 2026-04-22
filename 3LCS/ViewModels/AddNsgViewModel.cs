using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ThreeLCS.ViewModels
{
    public partial class AddNsgViewModel : ObservableObject
    {
        [ObservableProperty] private string _ruleName = string.Empty;
        [ObservableProperty] private string _ipOrCidr = string.Empty;

        public bool Confirmed { get; private set; }

        [RelayCommand]
        private void AddRule()
        {
            if (string.IsNullOrWhiteSpace(RuleName) || string.IsNullOrWhiteSpace(IpOrCidr)) return;
            Confirmed = true;
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
