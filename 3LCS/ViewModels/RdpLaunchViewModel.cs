using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Infrastructure;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class RdpLaunchViewModel : ObservableObject
    {
        private readonly ILcsEnvironmentService _envService;
        private readonly ILcsCredentialsService _credentialsService;
        private readonly ISettingsService _settings;
        private readonly ILogger<RdpLaunchViewModel> _logger;

        private CancellationTokenSource? _cts;

        [ObservableProperty] private string _statusText = "Initialising...";
        [ObservableProperty] private bool _isBusy = true;
        [ObservableProperty] private bool _isUserPickerVisible = false;
        [ObservableProperty] private bool _isCancelVisible = true;
        [ObservableProperty] private int _countdown = 0;
        [ObservableProperty] private bool _isCountdownVisible = false;

        public ObservableCollection<RDPConnectionDetails> RdpList { get; } = new();

        [ObservableProperty]
        private RDPConnectionDetails? _selectedConnection;

        private CloudHostedInstance? _instance;
        private EnvironmentRow? _row;
        private Window? _window;

        public RdpLaunchViewModel(
            ILcsEnvironmentService envService,
            ILcsCredentialsService credentialsService,
            ISettingsService settings,
            ILogger<RdpLaunchViewModel> logger)
        {
            _envService = envService;
            _credentialsService = credentialsService;
            _settings = settings;
            _logger = logger;
        }

        public void Initialise(EnvironmentRow row, Window window)
        {
            _row = row;
            _instance = row.Instance;
            _window = window;
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                var instance = _instance!;

                // ── Step 1: Start if stopped / unreachable ─────────────────────────
                bool isStopped = instance.DeploymentState == DeploymentState.Stopped;
                bool isUnreachable = _row!.Liveness == LivenessStatus.Unreachable;
                bool isAlreadyStarting = instance.DeploymentState == DeploymentState.Starting;

                if (isStopped || isUnreachable || isAlreadyStarting)
                {
                    if (!isAlreadyStarting)
                    {
                        StatusText = $"Sending start request for '{instance.DisplayName}'...";
                        bool ok = await _envService.StartStopDeploymentAsync(instance, "start");
                        if (!ok)
                        {
                            StatusText = "LCS rejected the start request.";
                            IsBusy = false;
                            return;
                        }
                    }

                    // ── Poll until Active (max 24 × 30 s = 12 min) ────────────────
                    bool becameActive = false;
                    for (int i = 0; i < 24 && !ct.IsCancellationRequested; i++)
                    {
                        StatusText = $"Waiting for '{instance.DisplayName}' to start... ({i * 30}s elapsed)";
                        await Task.Delay(TimeSpan.FromSeconds(30), ct);

                        var action = await _envService.GetOngoingActionDetailsAsync(instance);
                        if (action != null && IsActionInProgress(action.Status))
                        {
                            StatusText = $"Starting '{instance.DisplayName}'… {action.ActionStatusText ?? action.Status.ToString()} ({(i + 1) * 30}s)";
                            continue;
                        }

                        var freshList = await _envService.GetCheInstancesAsync();
                        var fresh = freshList.FirstOrDefault(x => x.EnvironmentId == instance.EnvironmentId);
                        if (fresh != null)
                        {
                            instance = fresh;
                            await Application.Current.Dispatcher.InvokeAsync(() => _row.Instance = fresh);
                            if (fresh.DeploymentState == DeploymentState.Active)
                            {
                                becameActive = true;
                                break;
                            }
                        }
                    }

                    ct.ThrowIfCancellationRequested();

                    if (!becameActive)
                    {
                        StatusText = "Machine did not become active within 12 minutes.";
                        IsBusy = false;
                        return;
                    }
                }

                // ── Step 2: Fetch credentials ──────────────────────────────────────
                StatusText = "Fetching credentials...";
                var rdpList = await Task.Run(() => _credentialsService.GetRdpConnectionDetails(instance), ct);

                if (rdpList.Count == 0)
                {
                    StatusText = "No RDP credentials available for this environment.";
                    IsBusy = false;
                    return;
                }

                // ── Step 3: Choose user ────────────────────────────────────────────
                RDPConnectionDetails? selected = null;

                if (_settings.AlwaysLogAsAdmin)
                {
                    selected = rdpList.Find(r => r.Username?.StartsWith("Admin", StringComparison.OrdinalIgnoreCase) == true);
                    if (selected == null)
                    {
                        // No admin account — fall through to picker
                        StatusText = "No admin account found — please select an account:";
                    }
                }

                if (selected == null)
                {
                    if (rdpList.Count == 1)
                    {
                        selected = rdpList[0];
                    }
                    else
                    {
                        // Show the picker inside the window
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var r in rdpList) RdpList.Add(r);
                            SelectedConnection = RdpList[0];
                            StatusText = "Select the account to connect with:";
                            IsUserPickerVisible = true;
                            IsBusy = false;
                        });

                        // Wait for the user to confirm
                        while (IsUserPickerVisible && !ct.IsCancellationRequested)
                            await Task.Delay(200, ct);

                        ct.ThrowIfCancellationRequested();
                        selected = SelectedConnection;
                        if (selected == null) return;
                        IsBusy = true;
                    }
                }

                // ── Step 4: Launch mstsc ───────────────────────────────────────────
                StatusText = $"Connecting as {selected.Username}...";

                await Task.Run(() =>
                {
                    using var creds = new RdpCredentials(selected.Address!, selected.Username!, selected.Password!);
                    var mstsc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
                            Arguments = $"/v:{selected.Address}:{selected.Port}"
                        }
                    };
                    mstsc.Start();
                }, ct);

                // ── Step 5: Countdown and close ────────────────────────────────────
                IsBusy = false;
                IsCancelVisible = false;
                IsCountdownVisible = true;
                for (int i = 5; i >= 1; i--)
                {
                    Countdown = i;
                    StatusText = $"RDP launched. This window closes in {i}s...";
                    await Task.Delay(1000, ct);
                }

                await Application.Current.Dispatcher.InvokeAsync(() => _window?.Close());
            }
            catch (OperationCanceledException)
            {
                StatusText = "Cancelled.";
                IsBusy = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RDP launch failed");
                StatusText = $"Error: {ex.Message}";
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            // User clicked OK in the picker — hide picker to unblock RunAsync
            IsUserPickerVisible = false;
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
            Application.Current.Dispatcher.Invoke(() => _window?.Close());
        }

        private static bool IsActionInProgress(LcsEnvironmentActionStatus s) =>
            s is LcsEnvironmentActionStatus.InProgress
              or LcsEnvironmentActionStatus.InProgressManually
              or LcsEnvironmentActionStatus.PreparingEnvironment;
    }
}
