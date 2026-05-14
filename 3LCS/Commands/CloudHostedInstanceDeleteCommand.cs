using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceDeleteCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceDeleteCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public async Task ExecuteAsync(EnvironmentCommandContext context)
        {
            var rows = GetSelectedCheRows(context);
            Logger.LogDebug("DeleteEnvironment clicked. Count={Count}", rows.Count);
            if (rows.Count == 0) return;

            var names = string.Join("\n", rows.Select(r => $"  - {r.Instance.DisplayName}"));
            var prompt = rows.Count == 1
                ? $"Delete environment {rows[0].Instance.DisplayName}?"
                : $"Delete {rows.Count} selected environments?\n\n{names}";
            if (!Dialog.ShowConfirm(prompt)) return;

            var ok = await RunCheBatchAsync(context, "Delete Environment", rows, async row =>
            {
                var success = await _envService.DeleteEnvironmentAsync(row.Instance);
                if (!success)
                    throw new InvalidOperationException("LCS returned a failure response for the delete request.");
            });

            if (ok)
            {
                Dialog.ShowInfo(rows.Count == 1 ? "Environment deleted." : "Environments deleted.");
                await context.RefreshAsync();
            }
        }
    }
}
