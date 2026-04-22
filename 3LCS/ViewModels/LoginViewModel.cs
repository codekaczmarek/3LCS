using CommunityToolkit.Mvvm.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ILcsAuthService _authService;
        private readonly ILcsHttpClientService _http;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = "Please log in to LCS in the browser above — the app will detect login automatically.";

        public string LcsUrl => _http.LcsUrl;
        public bool LoginResult { get; private set; }

        public LoginViewModel(ILcsAuthService authService, ILcsHttpClientService http)
        {
            _authService = authService;
            _http = http;
        }

        /// <summary>
        /// Called from LoginWindow code-behind with cookies extracted from WebView2's cookie store.
        /// </summary>
        public async Task LoginWithCookiesAsync(CookieContainer cookies)
        {
            IsBusy = true;
            StatusText = "Applying session cookies…";
            try
            {
                await Task.Run(() =>
                {
                    if (cookies.GetCookies(new System.Uri(_http.LcsUrl)).Count > 0)
                        _authService.SetCookies(cookies);
                });

                if (_authService.IsLoggedIn)
                {
                    LoginResult = true;
                    StatusText = "Login successful!";
                    foreach (Window window in Application.Current.Windows)
                        if (window.DataContext == this) { window.DialogResult = true; break; }
                }
                else
                {
                    StatusText = "No valid LCS cookies found — please complete the sign-in in the browser above.";
                }
            }
            finally { IsBusy = false; }
        }
    }
}
