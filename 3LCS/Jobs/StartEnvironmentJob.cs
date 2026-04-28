using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Jobs
{
    public sealed class StartEnvironmentJob : BackgroundJob
    {
        private readonly ILcsEnvironmentService _envService;
        private readonly CloudHostedInstance _instance;

        public override string Name => "Start Environment";

        public StartEnvironmentJob(ILcsEnvironmentService envService, CloudHostedInstance instance)
        {
            _envService = envService;
            _instance = instance;
        }

        public override async Task ExecuteAsync(IJobContext context)
            => await _envService.StartStopDeploymentAsync(_instance, "start");
    }
}
