using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceStartCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceStartCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public async Task ExecuteAsync(EnvironmentCommandContext context)
        {
            var rows = GetSelectedCheRows(context);
            Logger.LogDebug("StartEnvironment clicked. Count={Count}", rows.Count);
            if (rows.Count == 0) return;

            var ok = await RunCheBatchAsync(context, "Start Environment", rows, async row =>
            {
                var success = await _envService.StartStopDeploymentAsync(row.Instance, "start");
                if (!success)
                    throw new InvalidOperationException("LCS rejected the start request.");
            });

            if (ok) await context.RefreshAsync();
        }
    }
}
