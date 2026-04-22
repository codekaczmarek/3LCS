using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsEnvironmentService : LcsServiceBase, ILcsEnvironmentService
    {
        public LcsEnvironmentService(ILcsHttpClientService http, ILcsSessionService sessionState, ILogger<LcsEnvironmentService> logger)
            : base(http, sessionState, logger) { }

        public async Task<List<CloudHostedInstance>> GetCheInstancesAsync()
        {
            var url = $"{Http.LcsUrl}/DeploymentPortal/GetDeployementDetails/{Http.LcsProjectId}?_={Ts()}";
            var body = await GetDirectJsonpAsync<System.Collections.Generic.Dictionary<string, CloudHostedInstance>>(url);
            var list = new List<CloudHostedInstance>();
            if (body != null) list.AddRange(body.Values.OrderBy(x => x.InstanceId));
            return list;
        }

        public async Task<List<CloudHostedInstance>> GetSaasInstancesAsync()
        {
            var url = Http.LcsProjectTypeId != ProjectType.ServiceFabricImplementation
                ? $"{Http.LcsUrl}/SaasDeployment/GetDeploymentSummary/{Http.LcsProjectId}?_={Ts()}"
                : $"{Http.LcsUrl}/ServiceFabricDeployment/GetDeploymentSummary/{Http.LcsProjectId}?_={Ts()}";

            var instances = await GetAsync<System.Collections.Generic.List<HostedInstance>>(url);
            var list = new List<CloudHostedInstance>();
            if (instances == null) return list;
            instances = instances.OrderBy(x => x.DisplayOrder).ToList();
            foreach (var item in instances)
            {
                if (item.DeploymentInstances == null) continue;
                foreach (var instance in item.DeploymentInstances)
                {
                    if (!instance.IsDeployed) continue;
                    var details = await GetHostedDeploymentDetailAsync(instance);
                    if (details != null) list.Add(details);
                }
            }
            return list;
        }

        public async Task<CloudHostedInstance?> GetHostedDeploymentDetailAsync(HostedDeploymentInstance instance)
        {
            var url = instance.DeploymentEnvironmentType != DeploymentEnvironmentType.MicrosoftManagedServiceFabric
                ? $"{Http.LcsUrl}/SaaSDeployment/GetDeploymentDetail/{Http.LcsProjectId}/?environmentId={instance.EnvironmentId}&_={Ts()}"
                : $"{Http.LcsUrl}/ServiceFabricDeployment/GetEnvironmentDetails/{Http.LcsProjectId}/?environmentId={instance.EnvironmentId}&_={Ts()}";
            return await GetAsync<CloudHostedInstance>(url);
        }

        public async Task<bool> StartStopDeploymentAsync(CloudHostedInstance instance, string action)
        {
            // LCS returns a raw "true"/"false" string (not a Response JSON object) for this endpoint.
            var formData = $"{action}=&ActivityId={instance.ActivityId}&ProductName={System.Net.WebUtility.UrlEncode(instance.ProductName)}&TopologyName={instance.TopologyName}&TopologyInstanceId={instance.InstanceId}&AzureSubscriptionId={instance.AzureSubscriptionId}&EnvironmentGroup=0&EnvironmentId={instance.EnvironmentId}";
            var body = await PostFormRawAsync($"{Http.LcsUrl}/DeploymentPortal/StartStopDeployment/{Http.LcsProjectId}", formData);
            return body?.Trim() == "true";
        }

        public async Task<bool> DeleteEnvironmentAsync(CloudHostedInstance instance)
        {
            var formData = $"delete=&ActivityId={instance.ActivityId}&ProductName={System.Net.WebUtility.UrlEncode(instance.ProductName)}&TopologyName={instance.TopologyName}&TopologyInstanceId={instance.InstanceId}&AzureSubscriptionId={instance.AzureSubscriptionId}&EnvironmentGroup=0&EnvironmentId={instance.EnvironmentId}&PreserveCustomerSignOff=false";
            var response = await PostFormResponseAsync($"{Http.LcsUrl}/Environment/DeleteEnvironment/{Http.LcsProjectId}", formData);
            return response?.Success == true;
        }

        public async Task<ActionDetails?> GetOngoingActionDetailsAsync(CloudHostedInstance instance)
            => await GetAsync<ActionDetails>($"{Http.LcsUrl}/Environment/GetOngoingActionDetails/{Http.LcsProjectId}?environmentId={instance.EnvironmentId}");

        public async Task<List<ActionDetails>> GetEnvironmentHistoryDetailsAsync(CloudHostedInstance instance)
        {
            var paging = new PagingParameters { DynamicPaging = new DynamicPaging { StartPosition = 0, ItemsRequested = 40 } };
            var url = $"{Http.LcsUrl}/Environment/GetEnvironmentHistoryDetails/{Http.LcsProjectId}?environmentId={instance.EnvironmentId}&_={Ts()}";
            var data = await PostJsonAsync<EnvironmentHistoryDetailsData>(url, paging);
            return data?.Results ?? new List<ActionDetails>();
        }

        public string GetEnvironmentDetailsUrl(CloudHostedInstance instance) =>
            $"{Http.LcsUrl}/V2/EnvironmentDetailsV3New/{Http.LcsProjectId}?EnvironmentId={instance.EnvironmentId}&IsDiagnosticsEnabledEnvironment={instance.IsDiagnosticsEnabledEnvironment}";

        public string GetEnvironmentMonitoringUrl(CloudHostedInstance instance) =>
            $"{Http.LcsDiagUrl}/Monitoring/Index/{Http.LcsProjectId}?environmentId={instance.EnvironmentId}";

        public string GetDetailedVersionInfoUrl(CloudHostedInstance instance) =>
            $"{Http.LcsDiagUrl}/BuildInfo/Index/{Http.LcsProjectId}?lcsEnvironmentId={instance.EnvironmentId}";

        public string GetEnvironmentChangeHistoryUrl(CloudHostedInstance instance) =>
            $"{Http.LcsUrl}/V2/EnvironmentHistory/{Http.LcsProjectId}?LcsEnvironmentName={System.Net.WebUtility.UrlEncode(instance.DisplayName)}&EnvironmentId={instance.EnvironmentId}&EnvironmentType={instance.SaasEnvironmentType}";

        public DeploymentEnvironmentType GetDeploymentEnvironmentTypeInfo(string environmentId)
        {
            var url = $"{Http.LcsUrl}/Environment/GetDeploymentEnvironmentTypeInfo/{Http.LcsProjectId}?environmentId={environmentId}&_={Ts()}";
            var response = GetResponseSync(url);
            if (response?.Success == true && response.Data != null &&
                System.Enum.TryParse(response.Data.ToString(), out DeploymentEnvironmentType envType))
                return envType;
            return DeploymentEnvironmentType.MicrosoftManagedIaas;
        }
    }
}