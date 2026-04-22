using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class CustomLinksWindow : Window
    {
        public CustomLinksWindow(CustomLinksViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
