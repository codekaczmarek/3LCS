using Microsoft.Extensions.Logging;
using System.Linq;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceLogonCommand : CloudHostedInstanceCommandBase
    {
        public CloudHostedInstanceLogonCommand(IDialogService dialog, ILogger logger)
            : base(dialog, logger) { }

        public void Execute(EnvironmentCommandContext context)
        {
            var rows = GetSelectedEnvironmentRows(context);
            Logger.LogDebug("LogonToApplication clicked. Count={Count}", rows.Count);
            if (rows.Count == 0) return;

            var opened = 0;
            foreach (var row in rows)
            {
                using var scope = context.BeginProjectScope(row);
                var link = row.Instance.NavigationLinks?.FirstOrDefault(l => l.DisplayName == "Log on to environment");
                if (link?.NavigationUri == null) continue;
                Logger.LogDebug("Opening logon URL: {Url}", link.NavigationUri);
                Infrastructure.WebBrowserHelper.OpenUri(link.NavigationUri);
                opened++;
            }

            if (opened == 0)
            {
                Dialog.ShowInfo(rows.Count == 1
                    ? "No logon URL available for this environment."
                    : "No logon URLs available for the selected environments.");
            }
        }
    }
}
