using ThreeLCS.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ThreeLCS.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        private readonly IConfiguration _config;
        private AppUserSettings _user = AppUserSettings.Load();

        public SettingsService(IConfiguration config)
        {
            _config = config;
        }

        private string Coalesce(string userValue, string configKey, string fallback)
            => !string.IsNullOrWhiteSpace(userValue) ? userValue : (_config[configKey] ?? fallback);

        public string LcsUrl
        {
            get => Coalesce(_user.LcsUrl, "Lcs:Url", "https://lcs.dynamics.com");
            set => _user.LcsUrl = value;
        }
        public string LcsUpdateUrl
        {
            get => Coalesce(_user.LcsUpdateUrl, "Lcs:UpdateUrl", "https://update.lcs.dynamics.com");
            set => _user.LcsUpdateUrl = value;
        }
        public string LcsDiagUrl
        {
            get => Coalesce(_user.LcsDiagUrl, "Lcs:DiagUrl", "https://diag.lcs.dynamics.com");
            set => _user.LcsDiagUrl = value;
        }
        public string LcsFixUrl
        {
            get => Coalesce(_user.LcsFixUrl, "Lcs:FixUrl", "https://fix.lcs.dynamics.com");
            set => _user.LcsFixUrl = value;
        }

        public bool AlwaysLogAsAdmin => bool.TryParse(_config["App:AlwaysLogAsAdmin"], out var v) && v;
        public bool AutoRefresh => bool.TryParse(_config["App:AutoRefresh"], out var v) && v;

        public string Cookie { get => _user.Cookie; set => _user.Cookie = value; }
        public string CachingStore { get => _user.CachingStore; set => _user.CachingStore = value; }
        public bool CachingEnabled { get => _user.CachingEnabled; set => _user.CachingEnabled = value; }
        public bool KeepCache { get => _user.KeepCache; set => _user.KeepCache = value; }
        public string LastProjectId { get => _user.LastProjectId; set => _user.LastProjectId = value; }
        public string LastProjectName { get => _user.LastProjectName; set => _user.LastProjectName = value; }
        public int LastProjectTypeId { get => _user.LastProjectTypeId; set => _user.LastProjectTypeId = value; }
        public bool LivenessCheckEnabled { get => _user.LivenessCheckEnabled; set => _user.LivenessCheckEnabled = value; }
        public void Save() => _user.Save();

        public void ClearSession()
        {
            _user.Cookie = string.Empty;
            _user.LastProjectId = string.Empty;
            _user.LastProjectName = string.Empty;
            _user.LastProjectTypeId = 0;
            _user.Save();
        }
    }
}
