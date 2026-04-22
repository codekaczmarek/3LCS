using Newtonsoft.Json;
using System;
using System.IO;

namespace ThreeLCS.Services.Implementations
{
    public class AppUserSettings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "3LCS", "usersettings.json");

        public string Cookie { get; set; } = string.Empty;
        public string CachingStore { get; set; } = string.Empty;
        public bool CachingEnabled { get; set; } = true;
        public bool KeepCache { get; set; } = true;
        public string LastProjectId { get; set; } = string.Empty;
        public string LastProjectName { get; set; } = string.Empty;
        public int LastProjectTypeId { get; set; }
        public string LcsUrl { get; set; } = string.Empty;
        public string LcsUpdateUrl { get; set; } = string.Empty;
        public string LcsDiagUrl { get; set; } = string.Empty;
        public string LcsFixUrl { get; set; } = string.Empty;
        public bool LivenessCheckEnabled { get; set; } = true;
        public bool AlwaysLogAsAdmin { get; set; } = false;
        public bool AutoRefresh { get; set; } = false;

        public static AppUserSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonConvert.DeserializeObject<AppUserSettings>(File.ReadAllText(FilePath)) ?? new();
            }
            catch { }
            return new();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch { }
        }
    }
}
