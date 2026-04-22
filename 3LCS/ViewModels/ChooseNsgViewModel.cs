using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using ThreeLCS.Models;

namespace ThreeLCS.ViewModels
{
    public partial class ChooseNsgViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<NSGRule> _rules = new();
        [ObservableProperty] private NSGRule? _selectedRule;

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
