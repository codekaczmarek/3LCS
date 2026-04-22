using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class BuildInfoDetailsWindow : Window
    {
        private readonly BuildInfoDetailsViewModel _vm;

        public BuildInfoDetailsWindow(BuildInfoDetailsViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;
            Loaded += async (s, e) => await viewModel.LoadCommand.ExecuteAsync(null);
        }

        public CloudHostedInstance? Instance
        {
            set => _vm.Instance = value;
        }
    }
}
