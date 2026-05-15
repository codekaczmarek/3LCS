using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceOpenBuildInfoCommand : CloudHostedInstanceCommandBase
    {
        private readonly INavigationService _navigation;

        public CloudHostedInstanceOpenBuildInfoCommand(INavigationService navigation, IDialogService dialog, ILogger logger)
            : base(dialog, logger) => _navigation = navigation;

        public async Task ExecuteAsync(EnvironmentCommandContext context)
        {
            var rows = GetSelectedEnvironmentRows(context);
            Logger.LogDebug("OpenBuildInfo clicked. Count={Count}", rows.Count);
            foreach (var row in rows)
            {
                using var scope = context.BeginProjectScope(row);
                await _navigation.ShowViewBuildInfoAsync(row.Instance);
            }
        }
    }
}
