using System.Collections.Generic;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class ChooseMachineWindow : Window
    {
        private readonly ChooseMachineViewModel _viewModel;

        public ChooseMachineWindow(ChooseMachineViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        public IEnumerable<RDPConnectionDetails> Machines
        {
            set => _viewModel.Machines = new System.Collections.ObjectModel.ObservableCollection<RDPConnectionDetails>(value);
        }

        public RDPConnectionDetails? SelectedConnection => _viewModel.SelectedMachine;
    }
}
