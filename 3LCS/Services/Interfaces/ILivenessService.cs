using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILivenessService
    {
        /// <summary>
        /// Probes all environments in parallel (fire-and-forget friendly).
        /// Each <see cref="EnvironmentViewModel.Liveness"/> is updated as results arrive.
        /// </summary>
        Task CheckAllAsync(IEnumerable<EnvironmentViewModel> rows, CancellationToken ct = default);
    }
}
