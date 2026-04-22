using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
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
        private readonly IBackgroundTaskService _taskService;
        private readonly ILogger<RdpLaunchViewModel> _logger;

        private ManagedTask? _managedTask;

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
        private EnvironmentViewModel? _env;
        private Window? _window;

        // Polling intervals
        private const int TextRefreshSeconds = 1;
        private const int LivenessCheckSeconds = 3;
        private const int TimeoutMinutes = 12;
        // LCS takes time to reflect the new state after a start request.
        // Don't check for abort conditions until the first poll has had a chance to run.
        private const int StartGracePeriodSeconds = 65;

        public RdpLaunchViewModel(
            ILcsEnvironmentService envService,
            ILcsCredentialsService credentialsService,
            ISettingsService settings,
            MainViewModel mainViewModel,
            IBackgroundTaskService taskService,
            ILogger<RdpLaunchViewModel> logger)
        {
            _envService = envService;
            _credentialsService = credentialsService;
            _settings = settings;
            _mainViewModel = mainViewModel;
            _taskService = taskService;
            _logger = logger;
        }

        public void Initialise(EnvironmentViewModel env, Window window)
        {
            _env = env;
            _instance = env.Instance;
            _window = window;
            _managedTask = _taskService.Run(
                $"RDP Connect: {env.Instance.DisplayName}",
                RunAsync);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                var instance = _instance!;

                // ── Step 1: Start if stopped / unreachable ─────────────────────────
                bool isStopped = instance.DeploymentState == DeploymentState.Stopped;
                bool isUnreachable = _env!.Liveness == LivenessStatus.Unreachable;
                bool isAlreadyStarting = instance.DeploymentState == DeploymentState.Starting;

                if (isStopped || isUnreachable || isAlreadyStarting)
                {
                    // Refresh from LCS first to confirm the state before acting
                    if (isStopped || isUnreachable)
                    {
                        StatusText = $"Refreshing state of '{instance.DisplayName}'...";
                        var freshList = await _envService.GetCheInstancesAsync();
                        var fresh = freshList.FirstOrDefault(x => x.EnvironmentId == instance.EnvironmentId);
                        if (fresh != null)
                        {
                            instance = fresh;
                            await Application.Current.Dispatcher.InvokeAsync(() => _env!.Instance = fresh);
                            isStopped = fresh.DeploymentState == DeploymentState.Stopped;
                            isAlreadyStarting = fresh.DeploymentState == DeploymentState.Starting;
                            // If refresh shows it's active, skip the start-and-wait flow entirely
                            if (!isStopped && !isAlreadyStarting)
                                goto connectDirectly;
                        }
                    }

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

                        // Kick off MainViewModel's polling loop so MainWindow reflects state changes
                        await Application.Current.Dispatcher.InvokeAsync(() => _mainViewModel.TriggerDeploymentPolling());
                    }

                    // ── Wait loop: 1 s text refresh, 3 s TCP probe ─────────────────
                    // DeploymentState updates flow via MainViewModel.PollTransitionalStatesAsync
                    // (every 30 s) which writes to the same _env.Instance object.
                    var host = GetHost(instance);
                    bool becameReachable = false;
                    var sw = Stopwatch.StartNew();
                    var maxWait = TimeSpan.FromMinutes(TimeoutMinutes);
                    int secondsSinceLiveness = 0;

                    while (sw.Elapsed < maxWait && !ct.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(TextRefreshSeconds), ct);
                        secondsSinceLiveness++;

                        var elapsed = sw.Elapsed;
                        StatusText = $"Waiting for '{instance.DisplayName}' to start… {(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";

                        // Abort if MainViewModel's poller reports the environment went back to Stopped.
                        // Only check after the grace period so LCS has time to reflect the new state.
                        if (sw.Elapsed.TotalSeconds > StartGracePeriodSeconds)
                        {
                            var currentState = _env!.Instance.DeploymentState;
                            if (currentState == DeploymentState.Stopped || currentState == DeploymentState.Undefined)
                            {
                                StatusText = $"Start failed — machine returned to '{currentState}' state.";
                                IsBusy = false;
                                return;
                            }
                        }

                        // TCP liveness check every 3 seconds
                        if (host != null && secondsSinceLiveness >= LivenessCheckSeconds)
                        {
                            secondsSinceLiveness = 0;
                            if (await IsReachableAsync(host, ct))
                            {
                                becameReachable = true;
                                // Pick up the latest instance data written by MainViewModel's poller
                                instance = _env!.Instance;
                                break;
                            }
                        }
                    }

                    ct.ThrowIfCancellationRequested();

                    if (!becameReachable)
                    {
                        StatusText = $"Machine did not respond within {TimeoutMinutes} minutes.";
                        IsBusy = false;
                        return;
                    }
                }

                // ── Step 2: Fetch credentials ──────────────────────────────────────
                connectDirectly:
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
            IsUserPickerVisible = false;
        }

        [RelayCommand]
        private void Cancel()
        {
            _managedTask?.Cancel();
            Application.Current.Dispatcher.Invoke(() => _window?.Close());
        }

        private static async Task<bool> IsReachableAsync(string host, CancellationToken outerCt)
        {
            foreach (var port in new[] { 443, 80 })
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
                cts.CancelAfter(2500);
                try
                {
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(host, port, cts.Token);
                    return true;
                }
                catch { }
            }
            return false;
        }

        private static string? GetHost(CloudHostedInstance instance)
        {
            var link = instance.NavigationLinks?.FirstOrDefault(l => l.DisplayName == "Log on to environment");
            if (link?.NavigationUri == null) return null;
            return Uri.TryCreate(link.NavigationUri, UriKind.Absolute, out var uri) ? uri.Host : null;
        }
    }
}