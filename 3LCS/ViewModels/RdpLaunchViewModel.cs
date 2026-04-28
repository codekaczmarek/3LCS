using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private readonly IDeploymentCoordinator _coordinator;
        private readonly ILogger<RdpLaunchViewModel> _logger;

        private CancellationTokenSource _cts = new();

        [ObservableProperty] private string _statusText = "Initialising...";
        [ObservableProperty] private bool _isBusy = true;
        [ObservableProperty] private bool _isUserPickerVisible = false;
        [ObservableProperty] private bool _isCancelVisible = true;
        [ObservableProperty] private int _countdown = 0;
        [ObservableProperty] private bool _isCountdownVisible = false;

        public ObservableCollection<RDPConnectionDetails> RdpList { get; } = new();

        [ObservableProperty]
        private RDPConnectionDetails? _selectedConnection;

        private EnvironmentViewModel? _env;
        private Window? _window;

        private const int TimeoutMinutes = 12;
        // Give LCS at least 3 poll cycles (3 × 30 s = 90 s) to reflect the
        // Starting state before we consider aborting on a non-transitional reading.
        private const int StartGracePeriodSeconds = 100;

        public RdpLaunchViewModel(
            ILcsEnvironmentService envService,
            ILcsCredentialsService credentialsService,
            ISettingsService settings,
            IDeploymentCoordinator coordinator,
            ILogger<RdpLaunchViewModel> logger)
        {
            _envService = envService;
            _credentialsService = credentialsService;
            _settings = settings;
            _coordinator = coordinator;
            _logger = logger;
        }

        public void Initialise(EnvironmentViewModel env, Window window)
        {
            _env = env;
            _window = window;
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                var instance = _env!.Instance;

                // Step 1: Ensure the machine is running
                bool isStopped = instance.DeploymentState == DeploymentState.Stopped;
                bool isUnreachable = _env.Liveness == LivenessStatus.Unreachable;
                bool isStarting = IsTransitional(instance.DeploymentState);

                if (isStopped || isUnreachable || isStarting)
                {
                    bool alreadyReady = false;

                    // Refresh LCS state first to confirm before acting
                    if (isStopped || isUnreachable)
                    {
                        StatusText = $"Refreshing state of '{instance.DisplayName}'...";
                        var freshList = await _envService.GetCheInstancesAsync();
                        var fresh = freshList.FirstOrDefault(x => x.EnvironmentId == instance.EnvironmentId);
                        if (fresh != null)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() => _env!.Instance = fresh);
                            instance = fresh;
                            isStopped = fresh.DeploymentState == DeploymentState.Stopped;
                            alreadyReady = IsReadyToConnect(fresh.DeploymentState);
                        }
                    }

                    if (!alreadyReady)
                    {
                        if (isStopped)
                        {
                            // Register a named background task — identical to the MainWindow Start button
                            StatusText = $"Requesting start of '{instance.DisplayName}'...";
                            await Application.Current.Dispatcher.InvokeAsync(() => _coordinator.StartCheEnv(_env!));
                        }

                        // Ensure deployment polling is running (reuse existing loop if active)
                        await Application.Current.Dispatcher.InvokeAsync(() => _coordinator.EnsureDeploymentPolling());

                        // React to EnvironmentViewModel.Instance updates driven by the shared polling loop.
                        // Rather than busy-polling every second, we wait for the PropertyChanged event and
                        // only check state when the polling loop actually pushes a fresh value.
                        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        using var ctReg = ct.Register(() => tcs.TrySetCanceled(ct));
                        var sw = Stopwatch.StartNew();

                        void OnEnvChanged(object? sender, PropertyChangedEventArgs e)
                        {
                            if (e.PropertyName != nameof(EnvironmentViewModel.Instance)) return;
                            var state = _env!.Instance.DeploymentState;
                            if (IsReadyToConnect(state))
                                tcs.TrySetResult(true);
                            else if (sw.Elapsed.TotalSeconds > StartGracePeriodSeconds && !IsTransitional(state))
                                tcs.TrySetResult(false);
                        }

                        _env!.PropertyChanged += OnEnvChanged;
                        // Check current state immediately — polling may have already updated Instance
                        // before we subscribed, so PropertyChanged won't fire for the existing value.
                        OnEnvChanged(null, new PropertyChangedEventArgs(nameof(EnvironmentViewModel.Instance)));
                        try
                        {
                            // 1-second tick loop — only for updating the elapsed timer in the status text.
                            // State evaluation is driven exclusively by OnEnvChanged above.
                            while (!tcs.Task.IsCompleted)
                            {
                                if (sw.Elapsed >= TimeSpan.FromMinutes(TimeoutMinutes))
                                {
                                    tcs.TrySetResult(false);
                                    break;
                                }
                                try { await Task.Delay(1000, ct); }
                                catch (OperationCanceledException) { break; }

                                var state = _env!.Instance.DeploymentState;
                                StatusText = $"Waiting for '{instance.DisplayName}'... {(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s  |  LCS: {state}";
                            }

                            ct.ThrowIfCancellationRequested();

                            bool ready = await tcs.Task;
                            if (!ready)
                            {
                                var state = _env!.Instance.DeploymentState;
                                StatusText = sw.Elapsed >= TimeSpan.FromMinutes(TimeoutMinutes)
                                    ? $"Timed out after {TimeoutMinutes} minutes."
                                    : $"Start failed — environment entered '{state}' state.";
                                IsBusy = false;
                                return;
                            }

                            instance = _env!.Instance;
                        }
                        finally
                        {
                            _env!.PropertyChanged -= OnEnvChanged;
                        }
                    }
                }

                // Step 2: Fetch credentials
                StatusText = "Fetching credentials...";
                var rdpList = await Task.Run(() => _credentialsService.GetRdpConnectionDetails(instance), ct);

                if (rdpList.Count == 0)
                {
                    StatusText = "No RDP credentials available for this environment.";
                    IsBusy = false;
                    return;
                }

                // Step 3: Choose user
                RDPConnectionDetails? selected = null;

                if (_settings.AlwaysLogAsAdmin)
                {
                    selected = rdpList.Find(r => r.Username?.StartsWith("Admin", StringComparison.OrdinalIgnoreCase) == true);
                    if (selected == null)
                        StatusText = "No admin account found — please select an account:";
                }

                if (selected == null)
                {
                    if (rdpList.Count == 1)
                    {
                        selected = rdpList[0];
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var r in rdpList) RdpList.Add(r);
                            SelectedConnection = RdpList[0];
                            StatusText = "Select the account to connect with:";
                            IsUserPickerVisible = true;
                            IsBusy = false;
                        });

                        while (IsUserPickerVisible && !ct.IsCancellationRequested)
                            await Task.Delay(200, ct);

                        ct.ThrowIfCancellationRequested();
                        selected = SelectedConnection;
                        if (selected == null) return;
                        IsBusy = true;
                    }
                }

                // Step 4: Launch mstsc
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

                // Step 5: Countdown and close
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
        private void Confirm() => IsUserPickerVisible = false;

        [RelayCommand]
        private void Cancel()
        {
            _cts.Cancel();
            Application.Current.Dispatcher.Invoke(() => _window?.Close());
        }

        // "Starting" CHEs land in Finished when up; SaaS land in Active.
        // Accept any settled state that isn't clearly down/broken.
        private static bool IsReadyToConnect(DeploymentState state) =>
            !IsTransitional(state) && state is not (
                DeploymentState.Undefined or DeploymentState.Stopped or
                DeploymentState.Paused    or DeploymentState.Disabled or
                DeploymentState.Deleting  or DeploymentState.Deallocating or
                DeploymentState.Deallocated or DeploymentState.Deleted);

        private static bool IsTransitional(DeploymentState state) =>
            state is DeploymentState.Starting or DeploymentState.Stopping
                  or DeploymentState.Servicing or DeploymentState.Recovering
                  or DeploymentState.Restoring or DeploymentState.Deallocating;
    }
}
