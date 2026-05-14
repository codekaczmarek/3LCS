using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Commands
{
    public sealed class CloudHostedInstanceApplyPackageCommand : CloudHostedInstanceCommandBase
    {
        private readonly ILcsPackageService _packageService;
        private readonly INavigationService _navigation;

        public CloudHostedInstanceApplyPackageCommand(
            ILcsPackageService packageService,
            INavigationService navigation,
            IDialogService dialog,
            ILogger logger)
            : base(dialog, logger)
        {
            _packageService = packageService;
            _navigation = navigation;
        }

        public async Task ExecuteAsync(EnvironmentCommandContext context)
        {
            var rows = GetSelectedCheRows(context);
            var instance = rows.FirstOrDefault()?.Instance;
            Logger.LogDebug("ApplyPackage clicked. Count={Count}", rows.Count);
            if (instance == null) return;

            var package = await _navigation.ShowChoosePackageAsync(instance);
            if (package == null) return;

            var targetText = rows.Count == 1
                ? $"environment '{instance.DisplayName}'"
                : $"{rows.Count} selected environments:\n{string.Join("\n", rows.Select(r => $"  - {r.Instance.DisplayName}"))}";

            var confirmMsg =
                $"You are about to deploy the following package to {targetText}:\n\n" +
                $"  Package:    {package.Name}\n" +
                $"  Type:       {package.PackageType}\n" +
                $"  App ver.:   {package.AppVersion}\n" +
                $"  Platform:   {package.PlatformVersion}\n\n" +
                $"This operation may cause downtime and cannot be easily reverted.\n\n" +
                $"Are you sure you want to continue?";
            if (!Dialog.ShowConfirm(confirmMsg, "Confirm package deployment"))
            {
                Logger.LogDebug("ApplyPackage cancelled by user at confirmation prompt.");
                return;
            }

            var logs = new List<string>();
            var logsLock = new object();
            var ok = await RunCheBatchAsync(context, "Apply Package", rows, async row =>
            {
                var log = await Task.Run(() => _packageService.ApplyPackage(row.Instance, package));
                if (!string.IsNullOrEmpty(log))
                {
                    lock (logsLock)
                    {
                        logs.Add($"===== {row.Instance.DisplayName} =====\n{log}");
                    }
                }
            }, runSequentially: true);

            if (ok && logs.Count > 0)
                await _navigation.ShowLogDisplayAsync(string.Join("\n\n", logs));
        }
    }
}
