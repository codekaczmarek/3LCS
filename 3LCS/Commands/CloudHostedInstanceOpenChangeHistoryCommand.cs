using Microsoft.Extensions.Logging;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceOpenChangeHistoryCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceOpenChangeHistoryCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public void Execute(EnvironmentCommandContext context) =>
            OpenUrlsForSelectedEnvironments(context, nameof(CloudHostedInstanceOpenChangeHistoryCommand), row => _envService.GetEnvironmentChangeHistoryUrl(row.Instance));
    }
}
