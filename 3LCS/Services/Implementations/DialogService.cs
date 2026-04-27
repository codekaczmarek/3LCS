using Microsoft.Win32;
using System.Windows;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.Views;

namespace ThreeLCS.Services.Implementations
{
    public class DialogService : IDialogService
    {
        public void ShowInfo(string message, string title = "Information") =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void ShowError(string message, string title = "Error") =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public bool ShowConfirm(string message, string title = "Confirm") =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        public string? ShowInput(string prompt, string title, string currentValue = "")
        {
            var dialog = new InputDialog(prompt, title, currentValue) { Owner = Application.Current.MainWindow };
            return dialog.ShowDialog() == true ? dialog.Result : null;
        }

        public string? ShowSaveFileDialog(string defaultName, string filter)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultName,
                Filter = filter
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowOpenFileDialog(string filter)
        {
            var dialog = new OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
