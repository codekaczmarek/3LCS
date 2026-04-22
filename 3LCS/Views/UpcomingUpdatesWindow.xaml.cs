using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class UpcomingUpdatesWindow : Window
    {
        public UpcomingUpdatesWindow(UpcomingUpdatesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += async (s, e) => await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
