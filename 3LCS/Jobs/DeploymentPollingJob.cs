using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Jobs
{
    /// <summary>
    /// Polls LCS every 30 seconds while any environment is in a transitional deployment state
    /// (Starting, Stopping, or Servicing). Updates each <see cref="EnvironmentViewModel.Instance"/>
    /// in-place so the UI (tiles, DataGrid, open RDP windows) reacts to state changes without a
    /// full manual refresh.
    /// <para>
    /// The loop exits automatically once no environment remains transitional AND at least
    /// <see cref="minIterations"/> poll cycles have completed (the grace period prevents the loop
    /// from exiting before LCS has had time to reflect the transitional state after a Start/Stop
    /// request).
    /// </para>
    /// </summary>
    public sealed class DeploymentPollingJob : BackgroundJob
    {
        private readonly ILcsEnvironmentService _envService;
        private readonly ILogger _logger;
        private readonly List<EnvironmentViewModel> _rows;
        private readonly int _minIterations;

        public override string Name => "Deployment Polling";

        public DeploymentPollingJob(
            ILcsEnvironmentService envService,
            ILogger logger,
            List<EnvironmentViewModel> rows,
            int minIterations = 0)
        {
            _envService = envService;
            _logger = logger;
            _rows = rows;
            _minIterations = minIterations;
        }

        /// <summary>
        /// Returns true for states in which LCS is actively changing the environment's
        /// deployment. Shared with <see cref="EnvironmentViewModel.IsTransitional"/>.
        /// </summary>
        public static bool IsTransitionalState(DeploymentState state) =>
            state is DeploymentState.Starting or DeploymentState.Stopping or DeploymentState.Servicing;

        public override async Task ExecuteAsync(IJobContext context)
        {
            _logger.LogInformation(
                "Deployment state polling started for {Count} environment(s) (minIterations={Min})",
                _rows.Count, _minIterations);

            int iterations = 0;
            try
            {
                while (!context.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), context.Token);

                    var che = await _envService.GetCheInstancesAsync();
                    var saas = await _envService.GetSaasInstancesAsync();
                    var fresh = che.Concat(saas)
                        .ToDictionary(i => i.EnvironmentId ?? i.InstanceId ?? string.Empty);

                    bool anyStillTransitional = false;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var env in _rows)
                        {
                            var key = env.Instance.EnvironmentId ?? env.Instance.InstanceId ?? string.Empty;
                            if (!fresh.TryGetValue(key, out var updated)) continue;
                            env.Instance = updated;
                            if (IsTransitionalState(updated.DeploymentState))
                                anyStillTransitional = true;
                        }
                    });

                    iterations++;
                    _logger.LogInformation(
                        "Deployment state poll #{Iter} complete. AnyStillTransitional={Still}",
                        iterations, anyStillTransitional);

                    if (!anyStillTransitional && iterations >= _minIterations) break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Deployment state polling encountered an error — stopping");
            }
            finally
            {
                _logger.LogInformation("Deployment state polling stopped");
            }
        }
    }
}
