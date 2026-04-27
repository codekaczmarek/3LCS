using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class ChooseProjectWindow : Window
    {
        private readonly ChooseProjectViewModel _vm;

        public ChooseProjectWindow(ChooseProjectViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;
            Loaded += async (s, e) => await viewModel.LoadProjectsCommand.ExecuteAsync(null);
        }

        public LcsProject? SelectedProject => _vm.SelectedProject;

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.SelectedProject != null)
                _vm.SelectProjectCommand.Execute(null);
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
                row.IsSelected = true;
        }
    }
}
