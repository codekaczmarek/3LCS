using System;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Jobs
{
    public sealed class DeleteEnvironmentJob : BackgroundJob
    {
        private readonly ILcsEnvironmentService _envService;
        private readonly CloudHostedInstance _instance;

        public override string Name => "Delete Environment";

        public DeleteEnvironmentJob(ILcsEnvironmentService envService, CloudHostedInstance instance)
        {
            _envService = envService;
            _instance = instance;
        }

        public override async Task ExecuteAsync(IJobContext context)
        {
            bool success = await _envService.DeleteEnvironmentAsync(_instance);
            if (!success)
                throw new InvalidOperationException("LCS returned a failure response for the delete request.");
        }
    }
}
