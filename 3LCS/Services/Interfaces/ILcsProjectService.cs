using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsProjectService
    {
        Task<List<LcsProject>> GetAllProjectsAsync();
        Task<List<ProjectUser>> GetAllProjectUsersAsync();
        ProjectData? GetProject(string projectId);
        PlanData? RetrieveTenantPlans();
    }
}
