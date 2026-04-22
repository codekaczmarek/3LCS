using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface INavigationService
    {
        void ShowMainWindow();
        void ShowLoginWindow();
        Task<bool> ShowLoginWindowAsync();
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
        Task ShowPowerShellAsync(CloudHostedInstance instance);
        Task ShowCookieEditAsync();
        void CloseAll();
    }
}
