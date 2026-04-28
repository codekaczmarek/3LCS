using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Jobs
{
    /// <summary>
    /// Probes all environments in the supplied collection for TCP reachability (ports 443/80).
    /// Runs as an exclusive background task — starting a new instance cancels the previous one.
    /// </summary>
    public sealed class LivenessCheckJob : BackgroundJob
    {
        private readonly ILivenessService _livenessService;
        private readonly IEnumerable<EnvironmentViewModel> _rows;

        public override string Name => "Liveness Check";

        public LivenessCheckJob(ILivenessService livenessService, IEnumerable<EnvironmentViewModel> rows)
        {
            _livenessService = livenessService;
            _rows = rows;
        }

        public override Task ExecuteAsync(IJobContext context)
            => _livenessService.CheckAllAsync(_rows, context.Token);
    }
}
