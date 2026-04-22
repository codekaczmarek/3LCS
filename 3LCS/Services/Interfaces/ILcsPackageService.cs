using System.Collections.Generic;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsPackageService
    {
        List<DeployablePackage> GetPagedDeployablePackageList(CloudHostedInstance instance);
        string ApplyPackage(CloudHostedInstance instance, DeployablePackage package);
        Response ValidateSandboxServicing(DeployablePackage package);
        Response StartSandboxServicing(DeployablePackage package, string platformVersion);
    }
}
