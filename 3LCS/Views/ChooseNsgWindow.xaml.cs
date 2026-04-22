using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class ChooseNsgWindow : Window
    {
        private readonly ChooseNsgViewModel _vm;

        public ChooseNsgWindow(ChooseNsgViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;
        }

        public CloudHostedInstance? Instance
        {
            set
            {
                if (value?.Instances != null)
                {
                    // NSG rules must be loaded from a service; Instance stored for context only
                }
            }
        }
        public NSGRule? SelectedRule => _vm.SelectedRule;
    }
}
