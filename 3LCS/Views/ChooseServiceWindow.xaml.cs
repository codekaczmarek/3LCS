using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class ChooseServiceWindow : Window
    {
        public ChooseServiceWindow(ChooseServiceViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += async (s, e) => await viewModel.LoadAsync();
        }
    }
}
