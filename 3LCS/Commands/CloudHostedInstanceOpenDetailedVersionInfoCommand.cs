using Microsoft.Extensions.Logging;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceOpenDetailedVersionInfoCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceOpenDetailedVersionInfoCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public void Execute(EnvironmentCommandContext context) =>
            OpenUrlsForSelectedEnvironments(context, nameof(CloudHostedInstanceOpenDetailedVersionInfoCommand), row => _envService.GetDetailedVersionInfoUrl(row.Instance));
    }
}
