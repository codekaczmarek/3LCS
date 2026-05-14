using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Commands
{
    public abstract class CloudHostedInstanceCommandBase
    {
        protected readonly IDialogService Dialog;
        protected readonly ILogger Logger;

        protected CloudHostedInstanceCommandBase(IDialogService dialog, ILogger logger)
        {
            Dialog = dialog;
            Logger = logger;
        }

        protected static IReadOnlyList<EnvironmentViewModel> GetSelectedCheRows(EnvironmentCommandContext context)
        {
            if (context.SelectedTabIndex == 0)
                return context.ActiveCheRow == null ? new List<EnvironmentViewModel>() : new List<EnvironmentViewModel> { context.ActiveCheRow };

            if (context.SelectedCheRows.Count > 0)
                return context.SelectedCheRows.ToList();

            return context.SelectedCheRow == null ? new List<EnvironmentViewModel>() : new List<EnvironmentViewModel> { context.SelectedCheRow };
        }

        protected static IReadOnlyList<EnvironmentViewModel> GetSelectedEnvironmentRows(EnvironmentCommandContext context)
        {
            if (context.SelectedTabIndex == 2)
            {
                if (context.SelectedSaasRows.Count > 0)
                    return context.SelectedSaasRows.ToList();

                return context.SelectedSaasRow == null ? new List<EnvironmentViewModel>() : new List<EnvironmentViewModel> { context.SelectedSaasRow };
            }

            return GetSelectedCheRows(context);
        }

        protected void OpenUrlsForSelectedEnvironments(
            EnvironmentCommandContext context,
            string actionName,
            Func<EnvironmentViewModel, string> getUrl)
        {
            var rows = GetSelectedEnvironmentRows(context);
            Logger.LogDebug("{Action} clicked. Count={Count}", actionName, rows.Count);
            foreach (var row in rows)
            {
                using var scope = context.BeginProjectScope(row);
                var url = getUrl(row);
                Logger.LogDebug("Opening {Action} URL: {Url}", actionName, url);
                Infrastructure.WebBrowserHelper.OpenUri(url);
            }
        }

        protected async Task<bool> RunCheBatchAsync(
            EnvironmentCommandContext context,
            string actionName,
            IReadOnlyList<EnvironmentViewModel> rows,
            Func<EnvironmentViewModel, Task> action,
            bool runSequentially = false)
        {
            context.BusyHost.IsBusy = true;
            context.BusyHost.StatusText = rows.Count == 1 ? $"{actionName}\u2026" : $"{actionName} ({rows.Count})\u2026";
            try
            {
                var errors = runSequentially
                    ? await RunSequentiallyAsync(context, actionName, rows, action)
                    : await RunInParallelAsync(context, actionName, rows, action);
                if (errors.Count > 0)
                {
                    Dialog.ShowError(
                        rows.Count == 1
                            ? errors[0]!
                            : $"Some selected environments failed:\n\n{string.Join("\n", errors)}");
                    return false;
                }

                return true;
            }
            finally
            {
                context.BusyHost.IsBusy = false;
                context.BusyHost.StatusText = "Ready";
            }
        }

        private async Task<List<string?>> RunSequentiallyAsync(
            EnvironmentCommandContext context,
            string actionName,
            IReadOnlyList<EnvironmentViewModel> rows,
            Func<EnvironmentViewModel, Task> action)
        {
            var errors = new List<string?>();
            foreach (var row in rows)
            {
                var error = await RunForRowAsync(context, actionName, row, action);
                if (error != null)
                    errors.Add(error);
            }

            return errors;
        }

        private async Task<List<string?>> RunInParallelAsync(
            EnvironmentCommandContext context,
            string actionName,
            IReadOnlyList<EnvironmentViewModel> rows,
            Func<EnvironmentViewModel, Task> action)
        {
            var tasks = rows.Select(row => RunForRowAsync(context, actionName, row, action));
            return (await Task.WhenAll(tasks)).Where(error => error != null).ToList();
        }

        private async Task<string?> RunForRowAsync(
            EnvironmentCommandContext context,
            string actionName,
            EnvironmentViewModel row,
            Func<EnvironmentViewModel, Task> action)
        {
            try
            {
                using var scope = context.BeginProjectScope(row);
                await action(row);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "{Action} failed for {Environment}", actionName, row.Instance.DisplayName);
                return $"{row.Instance.DisplayName}: {ex.Message}";
            }
        }
    }
}
