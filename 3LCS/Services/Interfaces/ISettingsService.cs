namespace ThreeLCS.Services.Interfaces
{
    public interface ISettingsService
    {
        string LcsUrl { get; set; }
        string LcsUpdateUrl { get; set; }
        string LcsDiagUrl { get; set; }
        string LcsFixUrl { get; set; }
        bool LivenessCheckEnabled { get; set; }
        string Cookie { get; set; }
        string CachingStore { get; set; }
        bool CachingEnabled { get; set; }
        bool KeepCache { get; set; }
        bool AlwaysLogAsAdmin { get; set; }
        bool AutoRefresh { get; }
        string LastProjectId { get; set; }
        string LastProjectName { get; set; }
        int LastProjectTypeId { get; set; }
        void Save();
        void ClearSession();
    }
}
