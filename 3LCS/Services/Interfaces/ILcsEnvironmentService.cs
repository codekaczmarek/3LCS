using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsEnvironmentService
    {
        Task<List<CloudHostedInstance>> GetCheInstancesAsync();
        Task<List<CloudHostedInstance>> GetSaasInstancesAsync();
        Task<CloudHostedInstance?> GetHostedDeploymentDetailAsync(HostedDeploymentInstance instance);
        Task<bool> StartStopDeploymentAsync(CloudHostedInstance instance, string action);
        Task<bool> DeleteEnvironmentAsync(CloudHostedInstance instance);
        Task<ActionDetails?> GetOngoingActionDetailsAsync(CloudHostedInstance instance);
        Task<List<ActionDetails>> GetEnvironmentHistoryDetailsAsync(CloudHostedInstance instance);
        string GetEnvironmentDetailsUrl(CloudHostedInstance instance);
        string GetEnvironmentMonitoringUrl(CloudHostedInstance instance);
        string GetDetailedVersionInfoUrl(CloudHostedInstance instance);
        string GetEnvironmentChangeHistoryUrl(CloudHostedInstance instance);
        DeploymentEnvironmentType GetDeploymentEnvironmentTypeInfo(string environmentId);
    }
}
