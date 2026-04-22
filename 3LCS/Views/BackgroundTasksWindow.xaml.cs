using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class BackgroundTasksWindow : Window
    {
        public BackgroundTasksWindow(BackgroundTasksViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
