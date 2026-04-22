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

        // Polling intervals
        private const int TextRefreshSeconds = 1;
        private const int LivenessCheckSeconds = 3;
        private const int LcsStateCheckSeconds = 60;
        private const int TimeoutMinutes = 12;

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

                    // ── Wait loop: 1 s text refresh, 3 s TCP probe, 60 s LCS state ─
                    var host = GetHost(instance);
                    bool becameReachable = false;
                    var sw = Stopwatch.StartNew();
                    var maxWait = TimeSpan.FromMinutes(TimeoutMinutes);
                    int secondsSinceLiveness = 0;
                    int secondsSinceLcs = 0;

                    while (sw.Elapsed < maxWait && !ct.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(TextRefreshSeconds), ct);
                        secondsSinceLiveness++;
                        secondsSinceLcs++;

                        var elapsed = sw.Elapsed;
                        StatusText = $"Waiting for '{instance.DisplayName}' to start… {(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";

                        // TCP liveness check every 3 seconds
                        if (host != null && secondsSinceLiveness >= LivenessCheckSeconds)
                        {
                            secondsSinceLiveness = 0;
                            if (await IsReachableAsync(host, ct))
                            {
                                becameReachable = true;
                                // Refresh LCS state to get fresh instance data
                                var freshList = await _envService.GetCheInstancesAsync();
                                var fresh = freshList.FirstOrDefault(x => x.EnvironmentId == instance.EnvironmentId);
                                if (fresh != null)
                                {
                                    instance = fresh;
                                    await Application.Current.Dispatcher.InvokeAsync(() => _row.Instance = fresh);
                                }
                                break;
                            }
                        }

                        // LCS state check every 60 seconds (failure detection)
                        if (secondsSinceLcs >= LcsStateCheckSeconds)
                        {
                            secondsSinceLcs = 0;
                            var freshList = await _envService.GetCheInstancesAsync();
                            var fresh = freshList.FirstOrDefault(x => x.EnvironmentId == instance.EnvironmentId);
                            if (fresh != null)
                            {
                                instance = fresh;
                                await Application.Current.Dispatcher.InvokeAsync(() => _row.Instance = fresh);
                                // If it went back to Stopped or hit a terminal failure state — abort
                                if (fresh.DeploymentState == DeploymentState.Stopped
                                    || fresh.DeploymentState == DeploymentState.Undefined)
                                {
                                    StatusText = $"Start failed — machine returned to '{fresh.DeploymentState}' state.";
                                    IsBusy = false;
                                    return;
                                }
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
            _cts?.Cancel();
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