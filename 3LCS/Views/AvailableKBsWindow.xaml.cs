using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class AvailableKBsWindow : Window
    {
        private readonly AvailableKBsViewModel _vm;

        public AvailableKBsWindow(AvailableKBsViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;
        }

        public CloudHostedInstance? Instance
        {
            set
            {
                _vm.Instance = value;
                Loaded += async (s, e) => await _vm.LoadCommand.ExecuteAsync(null);
            }
        }
    }
}
