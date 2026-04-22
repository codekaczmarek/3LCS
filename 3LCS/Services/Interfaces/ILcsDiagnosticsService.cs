using System.Collections.Generic;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsDiagnosticsService
    {
        int GetBuildInfoEnvironmentId(CloudHostedInstance instance);
        BuildInfoDetails? GetEnvironmentBuildInfoDetails(CloudHostedInstance instance, string environmentId);
        List<Hotfix>? GetAvailableHotfixes(string envId, int hotfixesType);
        string? GetDiagEnvironmentId(CloudHostedInstance instance);
    }
}
