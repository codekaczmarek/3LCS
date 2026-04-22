using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using ThreeLCS.Models;

namespace ThreeLCS.ViewModels
{
    public partial class ChooseMachineViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<RDPConnectionDetails> _machines = new();
        [ObservableProperty] private RDPConnectionDetails? _selectedMachine;

        public ChooseMachineViewModel() { }

        public ChooseMachineViewModel(IEnumerable<RDPConnectionDetails> machines)
        {
            Machines = new ObservableCollection<RDPConnectionDetails>(machines);
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
