using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsProjectService : LcsServiceBase, ILcsProjectService
    {
        public LcsProjectService(ILcsHttpClientService http, ILcsSessionService sessionState, ILcsAuthService auth, ILogger<LcsProjectService> logger)
            : base(http, sessionState, auth, logger) { }

        public async Task<List<LcsProject>> GetAllProjectsAsync()
        {
            const int requested = 50;
            var all = new List<LcsProject>();
            var page = 0;
            int returned;
            do
            {
                page++;
                var paging = Paging(page, requested);
                var url = $"{Http.LcsUrl}/RainierProject/AllProjectsList";
                var data = await PostJsonAsync<ProjectsData>(url, paging);
                var projects = data?.Results ?? new();
                returned = projects.Count;
                all.AddRange(projects);
            }
            while (returned == requested);
            return all;
        }

        public async Task<List<ProjectUser>> GetAllProjectUsersAsync()
        {
            const int requested = 50;
            var all = new List<ProjectUser>();
            var page = 0;
            int returned;
            do
            {
                page++;
                var paging = Paging(page, requested);
                var url = $"{Http.LcsUrl}/RainierProjectUser/RetrieveProjectUsers/{Http.LcsProjectId}";
                var data = await PostJsonAsync<ProjectUsersData>(url, paging);
                var users = data?.Results ?? new();
                returned = users.Count;
                all.AddRange(users);
            }
            while (returned == requested);
            return all;
        }

        public ProjectData? GetProject(string projectId)
            => GetSync<ProjectData>($"{Http.LcsUrl}/RainierProject/GetProject/{projectId}?_={Ts()}");

        public PlanData? RetrieveTenantPlans()
            => GetSync<PlanData>($"{Http.LcsUrl}/RainierProject/RetrieveTenantPlans/{Http.LcsProjectId}?_={Ts()}");

        private static PagingParameters Paging(int page, int size) => new()
        {
            DynamicPaging = new DynamicPaging { StartPosition = page * size - size, ItemsRequested = size }
        };
    }
}
