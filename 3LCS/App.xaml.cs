using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Windows;
using ThreeLCS.Infrastructure;
using ThreeLCS.Services.Implementations;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.ViewModels;
using ThreeLCS.Views;

namespace ThreeLCS
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    // Infrastructure
                    services.AddSingleton<CookieContainer>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IFavouritesService, FavouritesService>();
                    services.AddSingleton<ILcsHttpClientService, LcsHttpClientService>();
                    services.AddSingleton<CredentialsCacheHelper>();

                    // Session state (registered before LCS services that depend on it)
                    services.AddSingleton<ILcsSessionService, LcsSessionService>();

                    // Services
                    services.AddSingleton<ILcsApiMonitorService, LcsApiMonitorService>();
                    services.AddSingleton<ILcsAuthService, LcsAuthService>();
                    services.AddSingleton<ILcsProjectService, LcsProjectService>();
                    services.AddSingleton<ILcsEnvironmentService, LcsEnvironmentService>();
                    services.AddSingleton<ILcsNsgService, LcsNsgService>();
                    services.AddSingleton<ILcsPackageService, LcsPackageService>();
                    services.AddSingleton<ILcsDiagnosticsService, LcsDiagnosticsService>();
                    services.AddSingleton<ILcsCredentialsService, LcsCredentialsService>();
                    services.AddSingleton<ILcsServiceRestartService, LcsServiceRestartService>();
                    services.AddSingleton<ILcsUpcomingUpdatesService, LcsUpcomingUpdatesService>();
                    services.AddSingleton<ILcsAssetLibraryService, LcsAssetLibraryService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<IRdpService, RdpService>();
                    services.AddSingleton<IExportService, ExportService>();
                    services.AddSingleton<ILivenessService, LivenessService>();
                    services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();
                    services.AddSingleton<IBackgroundJobRunner, BackgroundJobRunner>();
                    services.AddSingleton<INavigationService>(sp => new NavigationService(sp, sp.GetRequiredService<ILogger<NavigationService>>()));
                    services.AddSingleton<IUpdateService, UpdateService>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<IDeploymentCoordinator>(sp => sp.GetRequiredService<MainViewModel>());
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<ChooseProjectViewModel>();
                    services.AddTransient<AddNsgViewModel>();
                    services.AddTransient<AvailableKBsViewModel>();
                    services.AddTransient<BuildInfoDetailsViewModel>();
                    services.AddTransient<ChooseMachineViewModel>();
                    services.AddTransient<ChooseNsgViewModel>();
                    services.AddTransient<ChoosePackageViewModel>();
                    services.AddTransient<ChooseServiceViewModel>();
                    services.AddTransient<CookieEditViewModel>();
                    services.AddTransient<CredentialsViewModel>();
                    services.AddTransient<CustomLinksViewModel>();
                    services.AddTransient<EnvironmentChangesViewModel>();
                    services.AddTransient<LogDisplayViewModel>();
                    services.AddTransient<ParametersViewModel>();
                    services.AddTransient<PowerShellViewModel>();
                    services.AddTransient<RdpConnectViewModel>();
                    services.AddTransient<RdpLaunchViewModel>();
                    services.AddTransient<UpcomingUpdatesViewModel>();
                    services.AddTransient<BackgroundTasksViewModel>();
                    services.AddTransient<AssetLibrarySearchViewModel>();
                    services.AddTransient<AboutViewModel>();

                    // Windows
                    services.AddTransient<MainWindow>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<ChooseProjectWindow>();
                    services.AddTransient<AboutWindow>();
                    services.AddTransient<AddNsgWindow>();
                    services.AddTransient<AvailableKBsWindow>();
                    services.AddTransient<BuildInfoDetailsWindow>();
                    services.AddTransient<ChooseMachineWindow>();
                    services.AddTransient<ChooseNsgWindow>();
                    services.AddTransient<ChoosePackageWindow>();
                    services.AddTransient<ChooseServiceWindow>();
                    services.AddTransient<CookieEditWindow>();
                    services.AddTransient<CredentialsWindow>();
                    services.AddTransient<CustomLinksWindow>();
                    services.AddTransient<EnvironmentChangesWindow>();
                    services.AddTransient<LogDisplayWindow>();
                    services.AddTransient<ParametersWindow>();
                    services.AddTransient<PowerShellWindow>();
                    services.AddTransient<RdpConnectWindow>();
                    services.AddTransient<RdpLaunchWindow>();
                    services.AddTransient<UpcomingUpdatesWindow>();
                    services.AddTransient<BackgroundTasksWindow>();
                    services.AddTransient<AssetLibrarySearchWindow>();
                })
                .Build();
        }

        public static IServiceProvider Services => ((App)Current)._host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("3LCS starting up");

            // Ensure the session state service and API monitor are registered with the messenger before any service calls
            _ = _host.Services.GetRequiredService<ILcsSessionService>();
            _ = _host.Services.GetRequiredService<ILcsApiMonitorService>();

            if (UriHandler.DetectURILaunch(e.Args))
            {
                logger.LogInformation("URI launch detected");
                var rdpWindow = new RdpConnectWindow();
                rdpWindow.Show();
            }
            else
            {
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
                _ = CheckForUpdateAsync();
            }

            base.OnStartup(e);
        }

        private async System.Threading.Tasks.Task CheckForUpdateAsync()
        {
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            try
            {
                var updateService = _host.Services.GetRequiredService<IUpdateService>();
                var update = await updateService.CheckForUpdateAsync();
                if (update is null) return;

                var message = $"A new version of 3LCS is available: {update.Version}\n\nWould you like to open the release page to download it?";
                var result = MessageBox.Show(message, "Update Available",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    WebBrowserHelper.OpenUri(update.InstallerUrl ?? update.ReleaseUrl);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Update check could not start due to a service configuration error");
            }
            catch (WebException ex)
            {
                logger.LogWarning(ex, "Network error during update check");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                logger.LogWarning(ex, "HTTP error during update check");
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("3LCS shutting down");
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}

