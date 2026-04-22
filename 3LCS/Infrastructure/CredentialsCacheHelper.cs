using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Infrastructure
{
    public class CredentialsCacheHelper
    {
        private readonly ISettingsService _settings;

        public CredentialsCacheHelper(ISettingsService settings)
        {
            _settings = settings;
        }

        public void AddCredentialsCache(string environmentId, Dictionary<string, string> credentialDict) =>
            CacheUtil.Add(environmentId, credentialDict);

        public Dictionary<string, string>? GetCredentialsCache(string environmentId) =>
            CacheUtil.Get<Dictionary<string, string>>(environmentId);

        public void SaveCredentialsOffline()
        {
            var store = new CredentialsStore();
            MemoryCache.Default.ToList().ForEach(c =>
            {
                if (c.Value is Dictionary<string, string> creds)
                    store.EnvironmentCredentials.Add(new EnvironmentCredentials { EnvironmentId = c.Key, Credentials = creds });
            });

            var tempFile = _settings.CachingStore;
            if (string.IsNullOrEmpty(tempFile))
            {
                tempFile = Path.GetTempFileName();
                _settings.CachingStore = tempFile;
            }

            if (File.Exists(tempFile))
                File.WriteAllText(tempFile, JsonConvert.SerializeObject(store));
        }

        public void LoadOfflineCredentials()
        {
            var tempFile = _settings.CachingStore;
            if (string.IsNullOrEmpty(tempFile) || !File.Exists(tempFile)) return;
            try
            {
                var cache = File.ReadAllText(tempFile);
                var store = JsonConvert.DeserializeObject<CredentialsStore>(cache);
                store?.RebuildMemCache(this);
            }
            catch
            {
                _settings.CachingEnabled = false;
                _settings.CachingStore = string.Empty;
            }
        }
    }

    public class EnvironmentCredentials
    {
        public string EnvironmentId { get; set; } = string.Empty;
        public Dictionary<string, string> Credentials { get; set; } = new();
    }

    public class CredentialsStore
    {
        public List<EnvironmentCredentials> EnvironmentCredentials { get; set; } = new();

        public void RebuildMemCache(CredentialsCacheHelper helper)
        {
            foreach (var c in EnvironmentCredentials)
                helper.AddCredentialsCache(c.EnvironmentId, c.Credentials);
        }
    }
}
