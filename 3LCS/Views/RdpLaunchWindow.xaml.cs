using System.Windows;
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

        public void Launch(EnvironmentViewModel env)
        {
            _viewModel.Initialise(env, this);
        }
    }
}
