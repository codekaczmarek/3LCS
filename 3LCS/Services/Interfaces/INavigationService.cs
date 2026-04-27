using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Services.Interfaces
{
    public interface INavigationService
    {
        void ShowMainWindow();
        void ShowLoginWindow();
        Task<bool> ShowLoginWindowAsync();

        /// <summary>
        /// Performs headless WebView2-based SSO authentication without showing any UI.
        /// Returns a <see cref="CookieContainer"/> with fresh cookies if SSO succeeded, or null if it failed/timed out.
        /// </summary>
        Task<CookieContainer?> TryHeadlessSsoAsync();
        void ShowAbout();
        Task<LcsProject?> ShowChooseProjectAsync();
        Task<bool> ShowAddNsgRuleAsync(CloudHostedInstance instance);
        Task<NSGRule?> ShowChooseNsgAsync(CloudHostedInstance instance);
        Task ShowViewBuildInfoAsync(CloudHostedInstance instance);
        Task<DeployablePackage?> ShowChoosePackageAsync(CloudHostedInstance instance);
        Task ShowEnvironmentChangesAsync(CloudHostedInstance instance);
        Task ShowCredentialsAsync(CloudHostedInstance instance);
        Task ShowUpcomingUpdatesAsync();
        Task ShowCustomLinksAsync();
        Task ShowLogDisplayAsync(string logText);
        Task ShowAssetLibrarySearchAsync();
        Task ShowParametersAsync();
        Task ShowAvailableKBsAsync(CloudHostedInstance instance);
        Task ShowChooseMachineAsync(CloudHostedInstance instance);
        Task ShowRdpLaunchAsync(EnvironmentViewModel env);
        Task ShowBackgroundTasksAsync();
        Task ShowPowerShellAsync(CloudHostedInstance instance);
        Task ShowCookieEditAsync();
        void CloseAll();
    }
}
