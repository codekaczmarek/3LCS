using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsNsgService : LcsServiceBase, ILcsNsgService
    {
        private readonly ILcsEnvironmentService _envService;

        public LcsNsgService(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, ILcsEnvironmentService envService, ILogger<LcsNsgService> logger)
            : base(http, sessionState, auth, logger)
        {
            _envService = envService;
        }

        public NetworkSecurityGroup? GetNetworkSecurityGroup(CloudHostedInstance instance)
        {
            try
            {
                var isSF = _envService.GetDeploymentEnvironmentTypeInfo(instance.EnvironmentId!) == DeploymentEnvironmentType.MicrosoftManagedServiceFabric;
                var url = isSF
                    ? $"{Http.LcsUrl}/EnvironmentServicingV2/SFGetNetworkSecurityGroup/{Http.LcsProjectId}?lcsEnvironmentId={instance.EnvironmentId}&_={Ts()}"
                    : $"{Http.LcsUrl}/Environment/GetNetworkSecurityGroup/{Http.LcsProjectId}?lcsEnvironmentId={instance.EnvironmentId}&_={Ts()}";
                return GetSync<NetworkSecurityGroup>(url);
            }
            catch { return null; }
        }

        public async Task<bool> AddNsgRuleAsync(CloudHostedInstance instance, string ruleName, string ipOrCidr)
        {
            var isSF = _envService.GetDeploymentEnvironmentTypeInfo(instance.EnvironmentId!) == DeploymentEnvironmentType.MicrosoftManagedServiceFabric;
            string form, url;
            if (isSF)
            {
                form = $"lcsEnvironmentId={instance.EnvironmentId}&newRuleName={ruleName}&newRuleIpOrCidr={ipOrCidr}&newRuleService=AzureSQL";
                url = $"{Http.LcsUrl}/EnvironmentServicingV2/SFAddNetworkSecurityRule/{Http.LcsProjectId}";
            }
            else
            {
                form = $"lcsEnvironmentId={instance.EnvironmentId}&newRuleName={ruleName}&newRuleIpOrCidr={ipOrCidr}&newRuleService=RDP";
                url = $"{Http.LcsUrl}/Environment/AddNetworkSecurityRule/{Http.LcsProjectId}";
            }
            var r = await PostFormResponseAsync(url, form);
            return r?.Success == true;
        }

        public async Task<string> DeleteNsgRuleAsync(CloudHostedInstance instance, string rule)
        {
            var isSF = _envService.GetDeploymentEnvironmentTypeInfo(instance.EnvironmentId!) == DeploymentEnvironmentType.MicrosoftManagedServiceFabric;
            var url = isSF
                ? $"{Http.LcsUrl}/EnvironmentServicingV2/SFDeleteNetworkSecurityRules/{Http.LcsProjectId}"
                : $"{Http.LcsUrl}/Environment/DeleteNetworkSecurityRules/{Http.LcsProjectId}";
            var r = await PostFormResponseAsync(url, $"lcsEnvironmentId={instance.EnvironmentId}&rulesToDelete%5B%5D={rule}");
            return r?.Success == true
                ? $"Successfully deleted firewall rule {rule} for instance {instance.DisplayName}"
                : $"Could not delete firewall rule {rule} for instance {instance.DisplayName}";
        }
    }
}
