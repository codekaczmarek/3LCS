using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class RdpLaunchWindow : Window
    {
        private readonly RdpLaunchViewModel _viewModel;

        public RdpLaunchWindow(RdpLaunchViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        public void Launch(EnvironmentRow row)
        {
            _viewModel.Initialise(row, this);
        }
    }
}
