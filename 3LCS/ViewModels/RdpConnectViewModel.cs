using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using ThreeLCS.Infrastructure;

namespace ThreeLCS.ViewModels
{
    public partial class RdpConnectViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = "Connecting...";

        public string[]? Args { get; set; }

        [RelayCommand]
        private void Connect()
        {
            if (Args == null || Args.Length == 0) return;
            // Process URI arguments for RDP connection
            foreach (var arg in Args)
            {
                if (System.Uri.TryCreate(arg, System.UriKind.Absolute, out var uri) && uri.Scheme == UriHandler.URI_PROTOCOL_NAME.ToLower())
                {
                    StatusText = $"Processing URI: {uri}";
                    // URI-based RDP launch logic would go here
                }
            }
        }
    }
}
