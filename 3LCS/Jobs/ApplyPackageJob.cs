using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Jobs
{
    public sealed class ApplyPackageJob : BackgroundJob
    {
        private readonly ILcsPackageService _packageService;
        private readonly CloudHostedInstance _instance;
        private readonly DeployablePackage _package;

        public override string Name => "Apply Package";

        /// <summary>
        /// The deployment log returned by LCS.
        /// Null until <see cref="ExecuteAsync"/> completes without throwing.
        /// </summary>
        public string? Log { get; private set; }

        public ApplyPackageJob(ILcsPackageService packageService, CloudHostedInstance instance, DeployablePackage package)
        {
            _packageService = packageService;
            _instance = instance;
            _package = package;
        }

        public override async Task ExecuteAsync(IJobContext context)
            => Log = await System.Threading.Tasks.Task.Run(
                () => _packageService.ApplyPackage(_instance, _package),
                context.Token);
    }
}
