using Microsoft.Extensions.Logging;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceOpenMonitoringCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceOpenMonitoringCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public void Execute(EnvironmentCommandContext context) =>
            OpenUrlsForSelectedEnvironments(context, nameof(CloudHostedInstanceOpenMonitoringCommand), row => _envService.GetEnvironmentMonitoringUrl(row.Instance));
    }
}
