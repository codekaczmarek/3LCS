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
            Loaded += async (s, e) =>
            {
                await viewModel.LoadProjectsCommand.ExecuteAsync(null);
                SearchBox.Focus();
            };
        }

        public LcsProject? SelectedProject => _vm.SelectedProject;

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int count = ProjectGrid.Items.Count;
            if (count == 0) return;

            if (e.Key == Key.Down)
            {
                ProjectGrid.SelectedIndex = Math.Min(ProjectGrid.SelectedIndex + 1, count - 1);
                ProjectGrid.ScrollIntoView(ProjectGrid.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                ProjectGrid.SelectedIndex = Math.Max(ProjectGrid.SelectedIndex - 1, 0);
                ProjectGrid.ScrollIntoView(ProjectGrid.SelectedItem);
                e.Handled = true;
            }
        }

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
