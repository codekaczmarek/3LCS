using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.ViewModels;
using ThreeLCS.Views;

namespace ThreeLCS.Services.Implementations
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(IServiceProvider services, ILogger<NavigationService> logger)
        {
            _services = services;
            _logger = logger;
        }

        private T Resolve<T>() where T : notnull => (T)_services.GetService(typeof(T))!;

        public void ShowMainWindow()
        {
            _logger.LogDebug("Showing {Window}", nameof(MainWindow));
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<MainWindow>(); w.Show(); w.Activate(); });
        }

        public void ShowLoginWindow()
        {
            _logger.LogDebug("Showing {Window}", nameof(LoginWindow));
            Application.Current.Dispatcher.Invoke(() => Resolve<LoginWindow>().ShowDialog());
        }

        public Task<bool> ShowLoginWindowAsync() => Task.Run(() =>
        {
            _logger.LogDebug("Showing {Window}", nameof(LoginWindow));
            bool result = false;
            Application.Current.Dispatcher.Invoke(() => result = Resolve<LoginWindow>().ShowDialog() == true);
            return result;
        });

        public void ShowAbout()
        {
            _logger.LogDebug("Showing {Window}", nameof(AboutWindow));
            Application.Current.Dispatcher.Invoke(() => Resolve<AboutWindow>().ShowDialog());
        }

        public Task<LcsProject?> ShowChooseProjectAsync() => Task.Run(() =>
        {
            _logger.LogDebug("Showing {Window}", nameof(ChooseProjectWindow));
            LcsProject? result = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = Resolve<ChooseProjectWindow>();

                // Position the window just below the project switcher button in the toolbar
                if (Application.Current.MainWindow?.FindName("ProjectSwitcherButton") is FrameworkElement btn)
                {
                    var pt = btn.PointToScreen(new Point(0, btn.ActualHeight));
                    w.Left = pt.X;
                    w.Top  = pt.Y;
                    // Clamp so the window doesn't fall off the right or bottom edge
                    var work = SystemParameters.WorkArea;
                    if (w.Left + w.Width  > work.Right)  w.Left = work.Right  - w.Width;
                    if (w.Top  + w.Height > work.Bottom) w.Top  = work.Bottom - w.Height;
                }
                else
                {
                    w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    w.Owner = Application.Current.MainWindow;
                }

                if (w.ShowDialog() == true)
                    result = w.SelectedProject;
            });
            return result;
        });

        public Task<bool> ShowAddNsgRuleAsync(CloudHostedInstance instance) => Task.Run(() =>
        {
            bool result = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = Resolve<AddNsgWindow>();
                w.Instance = instance;
                result = w.ShowDialog() == true;
            });
            return result;
        });

        public Task<NSGRule?> ShowChooseNsgAsync(CloudHostedInstance instance) => Task.Run(() =>
        {
            NSGRule? result = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = Resolve<ChooseNsgWindow>();
                w.Instance = instance;
                w.ShowDialog();
                result = w.SelectedRule;
            });
            return result;
        });

        public Task ShowViewBuildInfoAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<BuildInfoDetailsWindow>(); w.Instance = instance; w.ShowDialog(); }));

        public Task<DeployablePackage?> ShowChoosePackageAsync(CloudHostedInstance instance) => Task.Run(() =>
        {
            DeployablePackage? result = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = Resolve<ChoosePackageWindow>();
                w.Instance = instance;
                if (w.ShowDialog() == true)
                    result = w.SelectedPackage;
            });
            return result;
        });

        public Task ShowEnvironmentChangesAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<EnvironmentChangesWindow>(); w.Instance = instance; w.ShowDialog(); }));

        public Task ShowCredentialsAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<CredentialsWindow>(); w.Instance = instance; w.ShowDialog(); }));

        public Task ShowUpcomingUpdatesAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<UpcomingUpdatesWindow>().ShowDialog()));

        public Task ShowCustomLinksAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<CustomLinksWindow>().ShowDialog()));

        public Task ShowLogDisplayAsync(string logText) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<LogDisplayWindow>(); w.LogText = logText; w.ShowDialog(); }));

        public Task ShowAssetLibrarySearchAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<AssetLibrarySearchWindow>().ShowDialog()));

        public Task ShowParametersAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<ParametersWindow>().ShowDialog()));

        public Task ShowAvailableKBsAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<AvailableKBsWindow>(); w.Instance = instance; w.ShowDialog(); }));

        public Task ShowRdpLaunchAsync(EnvironmentViewModel env)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = Resolve<RdpLaunchWindow>();
                w.Launch(env);
                w.Show();
            });
            return Task.CompletedTask;
        }

        public Task ShowBackgroundTasksAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = Resolve<BackgroundTasksWindow>();
                w.Owner = Application.Current.MainWindow;
                w.ShowDialog();
            }));

        public Task ShowChooseMachineAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<ChooseMachineWindow>().ShowDialog()));

        public Task ShowPowerShellAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<PowerShellWindow>(); w.Instance = instance; w.ShowDialog(); }));

        public Task ShowCookieEditAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<CookieEditWindow>().ShowDialog()));

        public Task<CookieContainer?> TryHeadlessSsoAsync()
        {
            _logger.LogDebug("Attempting headless WebView2 SSO");
            var http = Resolve<ILcsHttpClientService>();
            var lcsUrl = http.LcsUrl;

            var tcs = new TaskCompletionSource<CookieContainer?>();

            Application.Current.Dispatcher.BeginInvoke(new Func<Task>(async () =>
            {
                Window? window = null;
                WebView2? webView = null;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                cts.Token.Register(() => tcs.TrySetResult(null));
                try
                {
                    webView = new WebView2();
                    window = new Window
                    {
                        Title = "Signing in automatically… (3LCS)",
                        Width = 400,
                        Height = 200,
                        WindowStyle = WindowStyle.ToolWindow,
                        ShowInTaskbar = true,
                        ShowActivated = false,
                        WindowState = WindowState.Minimized,
                        ResizeMode = ResizeMode.NoResize,
                        Content = webView
                    };
                    window.Show();

                    await webView.EnsureCoreWebView2Async();

                    webView.CoreWebView2.NavigationCompleted += async (_, e) =>
                    {
                        var url = webView.Source?.ToString() ?? "";
                        if (e.IsSuccess && url.StartsWith($"{lcsUrl}/v2", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var wv2Cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync(lcsUrl);
                                var container = new CookieContainer();
                                var uri = new Uri(lcsUrl);
                                foreach (var c in wv2Cookies)
                                {
                                    try { container.Add(uri, new Cookie(c.Name, c.Value, c.Path)); }
                                    catch { /* skip malformed cookies */ }
                                }

                                _logger.LogInformation("Headless SSO succeeded — {Count} cookies extracted", wv2Cookies.Count);
                                tcs.TrySetResult(container);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to extract cookies from headless WebView2");
                                tcs.TrySetResult(null);
                            }
                        }
                    };

                    webView.CoreWebView2.Navigate($"{lcsUrl}/Logon/AdLogon");

                    // Wait for SSO or timeout
                    await tcs.Task;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Headless WebView2 SSO failed");
                    tcs.TrySetResult(null);
                }
                finally
                {
                    try { webView?.Dispose(); } catch { }
                    try { window?.Close(); } catch { }
                }
            }));

            return tcs.Task;
        }

        public void CloseAll()
        {
            _logger.LogDebug("Closing all windows");
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows) window.Close();
            });
        }
    }
}