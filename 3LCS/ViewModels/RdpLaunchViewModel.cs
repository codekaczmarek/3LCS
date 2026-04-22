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
        private readonly MainViewModel _mainViewModel;
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
        private const int StartGracePeriodSeconds = 65;

        public RdpLaunchViewModel(
            ILcsEnvironmentService envService,
            ILcsCredentialsService credentialsService,
            ISettingsService settings,
            MainViewModel mainViewModel,
            ILogger<RdpLaunchViewModel> logger)
        {
            _envService = envService;
            _credentialsService = credentialsService;
            _settings = settings;
            _mainViewModel = mainViewModel;
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
                            isStarting = IsTransitional(fresh.DeploymentState);
                            if (fresh.DeploymentState == DeploymentState.Active)
                                goto connectDirectly;
                        }
                    }

                    if (isStopped)
                    {
                        // Register a named background task — identical to the MainWindow Start button
                        StatusText = $"Requesting start of '{instance.DisplayName}'...";
                        await Application.Current.Dispatcher.InvokeAsync(() => _mainViewModel.StartCheEnv(_env!));
                    }
                    else
                    {
                        // Already in a transitional state — ensure deployment polling is active
                        await Application.Current.Dispatcher.InvokeAsync(() => _mainViewModel.ForceDeploymentPolling());
                    }

                    // Wait for EnvironmentViewModel.Instance.DeploymentState to reach Active.
                    // The state is updated every 30 s by MainViewModel's shared polling loop.
                    var sw = Stopwatch.StartNew();
                    var maxWait = TimeSpan.FromMinutes(TimeoutMinutes);

                    while (sw.Elapsed < maxWait)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(1000, ct);

                        var state = _env!.Instance.DeploymentState;
                        var elapsed = sw.Elapsed;
                        StatusText = $"Waiting for '{instance.DisplayName}'... {(int)elapsed.TotalMinutes}m {elapsed.Seconds}s  |  LCS: {state}";

                        if (state == DeploymentState.Active)
                        {
                            instance = _env.Instance;
                            break;
                        }

                        // After the grace period, abort if the state is settled and not Active
                        if (elapsed.TotalSeconds > StartGracePeriodSeconds && !IsTransitional(state))
                        {
                            StatusText = $"Start failed — environment entered '{state}' state.";
                            IsBusy = false;
                            return;
                        }
                    }

                    if (sw.Elapsed >= maxWait)
                    {
                        StatusText = $"Timed out after {TimeoutMinutes} minutes waiting for the environment to start.";
                        IsBusy = false;
                        return;
                    }
                }

                // Step 2: Fetch credentials
                connectDirectly:
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

        private static bool IsTransitional(DeploymentState state) =>
            state is DeploymentState.Starting or DeploymentState.Stopping
                  or DeploymentState.Servicing or DeploymentState.Recovering
                  or DeploymentState.Restoring or DeploymentState.Deallocating;
    }
}
