using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class CredentialsWindow : Window
    {
        private readonly CredentialsViewModel _vm;

        public CredentialsWindow(CredentialsViewModel viewModel)
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
