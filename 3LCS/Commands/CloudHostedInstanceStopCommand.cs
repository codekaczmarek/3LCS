using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceStopCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceStopCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public async Task ExecuteAsync(EnvironmentCommandContext context)
        {
            var rows = GetSelectedCheRows(context);
            Logger.LogDebug("StopEnvironment clicked. Count={Count}", rows.Count);
            if (rows.Count == 0) return;

            var ok = await RunCheBatchAsync(context, "Stop Environment", rows, async row =>
            {
                var success = await _envService.StartStopDeploymentAsync(row.Instance, "stop");
                if (!success)
                    throw new InvalidOperationException("LCS rejected the stop request.");
            });

            if (ok) await context.RefreshAsync();
        }
    }
}
