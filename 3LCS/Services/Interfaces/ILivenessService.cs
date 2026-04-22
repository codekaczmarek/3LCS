using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILivenessService
    {
        /// <summary>
        /// Probes all rows in parallel (fire-and-forget friendly).
        /// Each row's <see cref="EnvironmentRow.Liveness"/> is updated as results arrive.
        /// </summary>
        Task CheckAllAsync(IEnumerable<EnvironmentRow> rows, CancellationToken ct = default);
    }
}
