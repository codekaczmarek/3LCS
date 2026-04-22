using Microsoft.Web.WebView2.Core;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                WebView.CoreWebView2.Navigate($"{_viewModel.LcsUrl}/Logon/AdLogon");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var url = WebView.Source?.ToString() ?? string.Empty;
            if (e.IsSuccess && url.StartsWith($"{_viewModel.LcsUrl}/v2", StringComparison.OrdinalIgnoreCase)
                && !_viewModel.IsBusy)
            {
                await ExtractAndLoginAsync();
            }
        }

        private async void ManualExtract_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsBusy)
                await ExtractAndLoginAsync();
        }

        private async Task ExtractAndLoginAsync()
        {
            // Extract cookies directly from WebView2's Chromium cookie store.
            // NOTE: do NOT Uri.EscapeDataString the values — auth tokens contain base64 chars
            // that are valid in cookies; encoding them produces values the server won't recognise.
            var wv2Cookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync(_viewModel.LcsUrl);
            var container = new CookieContainer();
            var uri = new Uri(_viewModel.LcsUrl);
            foreach (var c in wv2Cookies)
            {
                try { container.Add(uri, new Cookie(c.Name, c.Value, c.Path)); }
                catch { /* skip malformed cookies */ }
            }
            await _viewModel.LoginWithCookiesAsync(container);
        }
    }
}
