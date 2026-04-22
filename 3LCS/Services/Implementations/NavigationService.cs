using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;
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
                w.ShowDialog();
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
                w.ShowDialog();
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

        public Task ShowChooseMachineAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<ChooseMachineWindow>().ShowDialog()));

        public Task ShowPowerShellAsync(CloudHostedInstance instance) => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => { var w = Resolve<PowerShellWindow>(); w.Instance = instance; w.ShowDialog(); }));

        public Task ShowCookieEditAsync() => Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() => Resolve<CookieEditWindow>().ShowDialog()));

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