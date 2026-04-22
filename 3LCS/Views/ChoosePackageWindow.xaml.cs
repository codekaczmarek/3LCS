using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class ChoosePackageWindow : Window
    {
        private readonly ChoosePackageViewModel _vm;

        public ChoosePackageWindow(ChoosePackageViewModel viewModel)
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

        public DeployablePackage? SelectedPackage => _vm.SelectedPackage;
    }
}
