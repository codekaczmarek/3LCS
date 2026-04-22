using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ThreeLCS.Infrastructure;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsCredentialsService : LcsServiceBase, ILcsCredentialsService
    {
        private readonly CredentialsCacheHelper _cache;
        private readonly ISettingsService _settings;

        public LcsCredentialsService(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, CredentialsCacheHelper cache, ISettingsService settings, ILogger<LcsCredentialsService> logger)
            : base(http, sessionState, auth, logger)
        {
            _cache = cache;
            _settings = settings;
        }

        public Dictionary<string, string>? GetCredentials(string environmentId, string itemName)
        {
            if (_settings.CachingEnabled)
            {
                var cached = _cache.GetCredentialsCache(environmentId);
                if (cached != null) return cached;
            }

            var url = $"{Http.LcsUrl}/DeploymentPortal/GetCredentials/{Http.LcsProjectId}?environmentId={environmentId}&deploymentItemName={itemName}&_={Ts()}";
            var data = GetSync<Dictionary<string, string>>(url);
            if (data != null && _settings.CachingEnabled)
                _cache.AddCredentialsCache(environmentId, data);
            return data;
        }

        public List<RDPConnectionDetails> GetRdpConnectionDetails(CloudHostedInstance instance)
        {
            var list = new List<RDPConnectionDetails>();
            if (instance.Instances == null) return list;

            foreach (var vm in instance.Instances)
            {
                // Check if RDP resource is available
                var checkUrl = $"{Http.LcsUrl}/DeploymentPortal/IsRdpResourceAvailable/{Http.LcsProjectId}/?topologyInstanceId={instance.InstanceId}&virtualMachineInstanceName={vm.MachineName}&deploymentItemName={vm.ItemName}&azureSubscriptionId={instance.AzureSubscriptionId}&group=0&isARMTopology={instance.IsARMTopology}&nsgWarningDisplayed=true&_={Ts()}";
                var checkResponse = GetResponseSync(checkUrl);
                if (checkResponse?.Success != true) continue;

                // Fetch the RDP file content (requires adjusted Accept headers)
                Http.HttpClient.DefaultRequestHeaders.Remove("Accept");
                Http.HttpClient.DefaultRequestHeaders.Remove("X-Requested-With");
                Http.HttpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                Http.HttpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                var rdpResult = Http.HttpClient.GetAsync(checkUrl + "&isDownloadEnabled=True").GetAwaiter().GetResult();
                rdpResult.EnsureSuccessStatusCode();
                var rdpBody = rdpResult.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var line1 = rdpBody.Split('\r', '\n').FirstOrDefault();

                Http.HttpClient.DefaultRequestHeaders.Remove("Accept");
                Http.HttpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                Http.HttpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                Http.HttpClient.DefaultRequestHeaders.Remove("Upgrade-Insecure-Requests");

                if (line1 == null) continue;
                var splited = line1.Split(':');
                vm.Credentials = GetCredentials(instance.EnvironmentId!, vm.ItemName!);
                if (vm.Credentials == null) continue;

                var localAdmin = vm.Credentials.Where(x => x.Key.Contains("Local admin")).ToDictionary(x => x.Key, x => x.Value);
                if (localAdmin.Count == 1)
                    list.Add(BuildRdpDetails(splited, localAdmin.First(), vm.MachineName!));

                var localUser = vm.Credentials.Where(x => x.Key.Contains("Local user")).ToDictionary(x => x.Key, x => x.Value);
                if (localUser.Count == 1)
                    list.Add(BuildRdpDetails(splited, localUser.First(), vm.MachineName!));
            }
            return list;
        }

        private static RDPConnectionDetails BuildRdpDetails(string[] splited, KeyValuePair<string, string> cred, string machineName)
        {
            var username = cred.Key.Split('\\').ElementAtOrDefault(1) ?? string.Empty;
            var parts = cred.Key.Split('\\').FirstOrDefault()?.Split('-') ?? Array.Empty<string>();
            var domain = parts.Length > 2 ? parts[2] : string.Empty;
            return new RDPConnectionDetails
            {
                Address = splited.ElementAtOrDefault(2),
                Port = splited.ElementAtOrDefault(3),
                Domain = domain,
                Username = username,
                Password = cred.Value,
                Machine = machineName
            };
        }
    }
}
