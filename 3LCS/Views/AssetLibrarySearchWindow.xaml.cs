using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class AssetLibrarySearchWindow : Window
    {
        public AssetLibrarySearchWindow(AssetLibrarySearchViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
