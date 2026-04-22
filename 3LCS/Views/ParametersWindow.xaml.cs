using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class ParametersWindow : Window
    {
        public ParametersWindow(ParametersViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
