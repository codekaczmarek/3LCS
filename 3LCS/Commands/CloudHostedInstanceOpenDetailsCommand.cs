using Microsoft.Extensions.Logging;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceOpenDetailsCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsEnvironmentService _envService;

        public CloudHostedInstanceOpenDetailsCommand(ILcsEnvironmentService envService, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _envService = envService;

        public void Execute(EnvironmentCommandContext context) =>
            OpenUrlsForSelectedEnvironments(context, nameof(CloudHostedInstanceOpenDetailsCommand), row => _envService.GetEnvironmentDetailsUrl(row.Instance));
    }
}
