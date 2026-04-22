using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ThreeLCS.ViewModels
{
    public partial class PowerShellViewModel : ObservableObject
    {
        [ObservableProperty] private string _script = string.Empty;
        [ObservableProperty] private string _output = string.Empty;
        [ObservableProperty] private bool _isBusy;

        [RelayCommand]
        private async Task Run()
        {
            IsBusy = true;
            Output = string.Empty;
            try
            {
                var result = await Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"{Script.Replace("\"", "\\\"")}\"")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(psi);
                    if (process == null) return "Failed to start PowerShell.";
                    sb.Append(process.StandardOutput.ReadToEnd());
                    sb.Append(process.StandardError.ReadToEnd());
                    process.WaitForExit();
                    return sb.ToString();
                });
                Output = result;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void Close()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.Close(); break; }
        }
    }
}
