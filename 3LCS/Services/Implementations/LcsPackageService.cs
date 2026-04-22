using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsPackageService : LcsServiceBase, ILcsPackageService
    {
        public LcsPackageService(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, ILogger<LcsPackageService> logger)
            : base(http, sessionState, auth, logger) { }

        public List<DeployablePackage> GetPagedDeployablePackageList(CloudHostedInstance instance)
        {
            const int requested = 50;
            var list = new List<DeployablePackage>();
            var page = 0;
            int returned;
            var endpoint = instance.RefinedEnvironmentType == RefinedEnvironmentType.DevTestDev ? "Environment" : "EnvironmentServicingV2";
            do
            {
                page++;
                var paging = new PagingParameters
                {
                    DynamicPaging = new DynamicPaging { StartPosition = page * requested - requested, ItemsRequested = requested }
                };
                var url = $"{Http.LcsUrl}/{endpoint}/GetPagedDeployablePackageList/{Http.LcsProjectId}?lcsEnvironmentActionId=2&lcsEnvironmentId={instance.EnvironmentId}";
                var data = PostJsonSync<PackagesData>(url, paging);
                var packages = data?.Results ?? new();
                returned = packages.Count;
                list.AddRange(packages);
            }
            while (returned == requested);
            return list;
        }

        public string ApplyPackage(CloudHostedInstance instance, DeployablePackage package)
        {
            var sb = new StringBuilder();
            package.LcsEnvironmentId = instance.EnvironmentId;
            var validation = ValidateSandboxServicing(package);
            if (validation.Success && validation.Data != null)
            {
                string platformRelease;
                try
                {
                    var releaseVersion = Newtonsoft.Json.JsonConvert.DeserializeObject<ValidateSandboxServicingData>(validation.Data.ToString()!);
                    platformRelease = releaseVersion?.PlatformRelease ?? validation.Data.ToString()!;
                }
                catch { platformRelease = validation.Data.ToString()!; }
                sb.AppendLine($"{instance.DisplayName}: Package deployment validation successful.");
                var deployment = StartSandboxServicing(package, platformRelease);
                sb.AppendLine($"{instance.DisplayName}: {deployment.Message}");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"{instance.DisplayName}: Package deployment validation failed.");
                sb.AppendLine($"{instance.DisplayName}: {validation.Message}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public Response ValidateSandboxServicing(DeployablePackage package)
            => PostFormResponseSync($"{Http.LcsUrl}/Environment/ValidateSandboxServicing/{Http.LcsProjectId}", BuildPackageParams(package))
               ?? new Response();

        public Response StartSandboxServicing(DeployablePackage package, string platformVersion)
            => PostFormResponseSync($"{Http.LcsUrl}/Environment/StartSandboxServicing/{Http.LcsProjectId}", BuildPackageParams(package) + $"&platformReleaseName={platformVersion}")
               ?? new Response();

        private static string BuildPackageParams(DeployablePackage p) =>
            $"package[PackageId]={p.PackageId}&package[Name]={p.Name}&package[Description]={p.Description}&package[packageType]={p.PackageType}&package[ModifiedDate]={p.ModifiedDate}&package[ModifiedBy]={p.ModifiedBy}&package[Publisher]={p.Publisher}&package[Scope]={p.Scope}&package[LcsEnvironmentActionId]={p.LcsEnvironmentActionId}&package[LcsEnvironmentId]={p.LcsEnvironmentId}&package[FileAssetDisplayVersion]={p.FileAssetDisplayVersion}&package[PlatformVersion]={p.PlatformVersion}&package[AppVersion]={p.AppVersion}&package[EstimatedDuration]={p.EstimatedDuration}";
    }
}
