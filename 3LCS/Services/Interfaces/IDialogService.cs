using System.Threading.Tasks;

namespace ThreeLCS.Services.Interfaces
{
    public interface IDialogService
    {
        void ShowInfo(string message, string title = "Information");
        void ShowError(string message, string title = "Error");
        bool ShowConfirm(string message, string title = "Confirm");
        string? ShowInput(string prompt, string title, string currentValue = "");
        string? ShowSaveFileDialog(string defaultName, string filter);
        string? ShowOpenFileDialog(string filter);
    }
}
