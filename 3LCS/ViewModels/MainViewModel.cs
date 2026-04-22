using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Messages;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<SessionStateChangedMessage>
    {
        private readonly ILcsEnvironmentService _envService;
        private readonly ILcsProjectService _projectService;
        private readonly ILcsHttpClientService _http;
        private readonly IRdpService _rdpService;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialog;
        private readonly IExportService _exportService;
        private readonly ISettingsService _settings;
        private readonly ILcsCredentialsService _credentialsService;
        private readonly ILcsNsgService _nsgService;
        private readonly ILcsDiagnosticsService _diagService;
        private readonly ILcsPackageService _packageService;
        private readonly ILcsAuthService _authService;
        private readonly ILcsSessionService _sessionState;
        private readonly ILivenessService _livenessService;
        private readonly ILogger<MainViewModel> _logger;
        private bool _autoLoginInProgress;
        private CancellationTokenSource? _livenessCts;
        private CancellationTokenSource? _pollingCts;
        private CancellationTokenSource? _rdpCts;

        public ILcsApiMonitorService ApiMonitor { get; }

        [ObservableProperty] private ObservableCollection<EnvironmentRow> _cheInstances = new();
        [ObservableProperty] private ObservableCollection<EnvironmentRow> _saasInstances = new();
        [ObservableProperty] private EnvironmentRow? _selectedCheRow;
        [ObservableProperty] private EnvironmentRow? _selectedSaasRow;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = "Not logged in. Use File → Login to LCS.";
        [ObservableProperty] private LcsProject? _selectedProject;
        [ObservableProperty] private string _windowTitle = "3LCS";
        [ObservableProperty] private bool _isLoggedIn;
        [ObservableProperty] private bool _isMonitorOpen;

        // Computed helpers used by all commands — no command body changes needed
        private CloudHostedInstance? SelectedCheInstance => SelectedCheRow?.Instance;
        private CloudHostedInstance? SelectedSaasInstance => SelectedSaasRow?.Instance;

        public MainViewModel(
            ILcsEnvironmentService envService,
            ILcsProjectService projectService,
            ILcsHttpClientService http,
            IRdpService rdpService,
            INavigationService navigation,
            IDialogService dialog,
            IExportService exportService,
            ISettingsService settings,
            ILcsCredentialsService credentialsService,
            ILcsNsgService nsgService,
            ILcsDiagnosticsService diagService,
            ILcsPackageService packageService,
            ILcsApiMonitorService apiMonitor,
            ILcsAuthService authService,
            ILcsSessionService sessionState,
            ILivenessService livenessService,
            ILogger<MainViewModel> logger)
        {
            _envService = envService;
            _projectService = projectService;
            _http = http;
            _rdpService = rdpService;
            _navigation = navigation;
            _dialog = dialog;
            _exportService = exportService;
            _settings = settings;
            _credentialsService = credentialsService;
            _nsgService = nsgService;
            _diagService = diagService;
            _packageService = packageService;
            ApiMonitor = apiMonitor;
            _authService = authService;
            _sessionState = sessionState;
            _livenessService = livenessService;
            _logger = logger;
            WeakReferenceMessenger.Default.Register<SessionStateChangedMessage>(this);
        }

        void IRecipient<SessionStateChangedMessage>.Receive(SessionStateChangedMessage message)
        {
            void Update()
            {
                IsLoggedIn = message.NewState == LcsSessionState.LoggedIn;
                if (message.NewState == LcsSessionState.SessionExpired)
                {
                    if (SelectedProject != null && !_autoLoginInProgress)
                    {
                        StatusText = "⚠️ Session expired — re-authenticating…";
                        _ = AutoReLoginAsync();
                    }
                    else if (SelectedProject == null)
                    {
                        StatusText = "⚠️ Session expired — please log in again (File → Login to LCS)";
                    }
                }
                else if (message.NewState == LcsSessionState.NotLoggedIn)
                    StatusText = "Not logged in. Use File → Login to LCS.";
            }
            if (Application.Current?.Dispatcher.CheckAccess() == true) Update();
            else Application.Current?.Dispatcher.InvokeAsync(Update);
        }

        private async Task AutoReLoginAsync()
        {
            _autoLoginInProgress = true;
            try
            {
                _logger.LogInformation("Session expired with active project — showing login window automatically");
                var loggedIn = await _navigation.ShowLoginWindowAsync();
                if (!loggedIn)
                {
                    StatusText = "⚠️ Login cancelled — please log in again (File → Login to LCS)";
                    return;
                }
                StatusText = $"Re-authenticated. Loading {SelectedProject!.Name}…";
                try
                {
                    await Refresh();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Refresh after re-login failed — session may still be invalid");
                    StatusText = "⚠️ Could not load data after re-login — please try logging in again";
                }
            }
            finally
            {
                // Reset the guard on the UI thread so it is always cleared AFTER any pending
                // SessionStateChangedMessage dispatched via Dispatcher.InvokeAsync is processed.
                // This prevents the race where _autoLoginInProgress becomes false before the
                // SessionExpired message handler runs, which would open a second login window.
                await Application.Current.Dispatcher.InvokeAsync(() => { _autoLoginInProgress = false; });
            }
        }

        /// <summary>Called from MainWindow.Loaded — restores saved session if available.</summary>
        public async Task InitializeAsync()
        {
            if (_authService.RestoreSavedCookies())
            {
                if (!string.IsNullOrEmpty(_settings.LastProjectId))
                {
                    SelectedProject = new LcsProject
                    {
                        Id = int.TryParse(_settings.LastProjectId, out var id) ? id : 0,
                        Name = _settings.LastProjectName,
                        ProjectTypeId = (ProjectType)_settings.LastProjectTypeId
                    };
                    _http.ChangeLcsProjectId(_settings.LastProjectId);
                    _http.LcsProjectTypeId = SelectedProject.ProjectTypeId;
                    WindowTitle = $"3LCS — {SelectedProject.Name}";
                    StatusText = $"Session restored. Project: {SelectedProject.Name}";
                    await Refresh();
                }
                else
                {
                    StatusText = "Logged in. Choose a project via File → Choose Project.";
                }
            }
        }

        partial void OnIsLoggedInChanged(bool value)
        {
            RefreshCommand.NotifyCanExecuteChanged();
            ChooseProjectCommand.NotifyCanExecuteChanged();
            OpenRdpCommand.NotifyCanExecuteChanged();
            DeleteEnvironmentCommand.NotifyCanExecuteChanged();
            StartEnvironmentCommand.NotifyCanExecuteChanged();
            StopEnvironmentCommand.NotifyCanExecuteChanged();
            AddNsgRuleCommand.NotifyCanExecuteChanged();
            DeleteNsgRuleCommand.NotifyCanExecuteChanged();
            ApplyPackageCommand.NotifyCanExecuteChanged();
            ExportToCsvCommand.NotifyCanExecuteChanged();
            ExportToRdcManCommand.NotifyCanExecuteChanged();
        }

        // ── Auth ───────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task LoginToLcs()
        {
            var loggedIn = await _navigation.ShowLoginWindowAsync();
            if (!loggedIn) return;
            if (SelectedProject != null)
            {
                StatusText = $"Re-authenticated. Loading {SelectedProject.Name}…";
                await Refresh();
            }
            else
            {
                StatusText = "Logged in successfully. Choose a project.";
                await ChooseProject();
            }
        }

        [RelayCommand]
        private void Logout()
        {
            if (!_dialog.ShowConfirm("Log out and clear saved session?")) return;
            _authService.Logout();
            SelectedProject = null;
            WindowTitle = "3LCS";
            CancelLiveness();
            CancelPolling();
            CheInstances.Clear();
            SaasInstances.Clear();
        }

        // ── Commands ───────────────────────────────────────────────────────────

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task Refresh()
        {
            if (SelectedProject == null)
            {
                StatusText = "No project selected. Use File → Choose Project.";
                return;
            }
            IsBusy = true;
            StatusText = "Loading environments...";
            CancelPolling();
            try
            {
                var che = await _envService.GetCheInstancesAsync();
                var saas = await _envService.GetSaasInstancesAsync();

                var cheRows = che.Select(i => new EnvironmentRow(i)).ToList();
                var saasRows = saas.Select(i => new EnvironmentRow(i)).ToList();

                CheInstances = new ObservableCollection<EnvironmentRow>(cheRows);
                SaasInstances = new ObservableCollection<EnvironmentRow>(saasRows);
                StatusText = $"Loaded {CheInstances.Count} CHE and {SaasInstances.Count} SaaS instances.";

                var allRows = cheRows.Concat(saasRows).ToList();
                if (_settings.LivenessCheckEnabled)
                    FireLivenessCheck(allRows);
                StartOrStopPolling(allRows);
            }
            catch (Exception ex)
            {
                // 498 = session expired; SessionStateChangedMessage triggers AutoReLoginAsync — no dialog needed
                var is498 = ex is HttpRequestException hre && (int?)hre.StatusCode == 498;
                if (is498)
                {
                    StatusText = "⚠️ Session expired";
                }
                else
                {
                    StatusText = $"Error: {ex.Message}";
                    _dialog.ShowError(ex.Message);
                }
            }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task ChooseProject()
        {
            var project = await _navigation.ShowChooseProjectAsync();
            if (project != null)
            {
                SelectedProject = project;
                _http.ChangeLcsProjectId(project.Id.ToString());
                _http.LcsProjectTypeId = project.ProjectTypeId;
                WindowTitle = $"3LCS — {project.Name}";
                // Persist for next startup
                _settings.LastProjectId = project.Id.ToString();
                _settings.LastProjectName = project.Name ?? string.Empty;
                _settings.LastProjectTypeId = (int)project.ProjectTypeId;
                _settings.Save();
                await Refresh();
            }
        }


        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task OpenRdp()
        {
            var row = SelectedCheRow;
            _logger.LogDebug("OpenRdp clicked. SelectedCheInstance={Instance}", row?.Instance?.DisplayName ?? "<null>");
            if (row == null) return;

            _rdpCts?.Cancel();
            _rdpCts?.Dispose();
            _rdpCts = new CancellationTokenSource();

            await _navigation.ShowRdpLaunchAsync(row);
        }

        [RelayCommand]
        private void LogonToApplication()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("LogonToApplication clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            var link = instance.NavigationLinks?.FirstOrDefault(l => l.DisplayName == "Log on to environment");
            if (link?.NavigationUri != null)
            {
                _logger.LogDebug("Opening logon URL: {Url}", link.NavigationUri);
                Infrastructure.WebBrowserHelper.OpenUri(link.NavigationUri);
            }
            else
            {
                _dialog.ShowInfo("No logon URL available for this environment.");
            }
        }

        [RelayCommand]
        private void OpenInstanceDetails()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("OpenInstanceDetails clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            var url = _envService.GetEnvironmentDetailsUrl(instance);
            _logger.LogDebug("Opening instance details URL: {Url}", url);
            Infrastructure.WebBrowserHelper.OpenUri(url);
        }

        [RelayCommand]
        private void OpenEnvironmentMonitoring()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("OpenEnvironmentMonitoring clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            Infrastructure.WebBrowserHelper.OpenUri(_envService.GetEnvironmentMonitoringUrl(instance));
        }

        [RelayCommand]
        private void OpenDetailedVersionInfo()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("OpenDetailedVersionInfo clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            Infrastructure.WebBrowserHelper.OpenUri(_envService.GetDetailedVersionInfoUrl(instance));
        }

        [RelayCommand]
        private void OpenEnvironmentChangeHistory()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("OpenEnvironmentChangeHistory clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            Infrastructure.WebBrowserHelper.OpenUri(_envService.GetEnvironmentChangeHistoryUrl(instance));
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task DeleteEnvironment()
        {
            var instance = SelectedCheInstance;
            _logger.LogDebug("DeleteEnvironment clicked. SelectedCheInstance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            if (!_dialog.ShowConfirm($"Delete environment {instance.DisplayName}?")) return;
            IsBusy = true;
            try
            {
                var success = await _envService.DeleteEnvironmentAsync(instance);
                _dialog.ShowInfo(success ? "Environment deleted." : "Failed to delete environment.");
                if (success) await Refresh();
            }
            catch (Exception ex) { _dialog.ShowError(ex.Message); }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task StartEnvironment()
        {
            var instance = SelectedCheInstance;
            _logger.LogDebug("StartEnvironment clicked. SelectedCheInstance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            IsBusy = true;
            try { await _envService.StartStopDeploymentAsync(instance, "start"); await Refresh(); }
            catch (Exception ex) { _dialog.ShowError(ex.Message); }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task StopEnvironment()
        {
            var instance = SelectedCheInstance;
            _logger.LogDebug("StopEnvironment clicked. SelectedCheInstance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            IsBusy = true;
            try { await _envService.StartStopDeploymentAsync(instance, "stop"); await Refresh(); }
            catch (Exception ex) { _dialog.ShowError(ex.Message); }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task AddNsgRule()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("AddNsgRule clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            await _navigation.ShowAddNsgRuleAsync(instance);
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task DeleteNsgRule()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            if (instance == null) return;
            var rule = await _navigation.ShowChooseNsgAsync(instance);
            if (rule == null) return;
            IsBusy = true;
            try
            {
                var result = await _nsgService.DeleteNsgRuleAsync(instance, rule.Name ?? string.Empty);
                _dialog.ShowInfo(string.IsNullOrEmpty(result) ? "NSG rule deleted." : result);
            }
            catch (Exception ex) { _dialog.ShowError(ex.Message); }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task ApplyPackage()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            _logger.LogDebug("ApplyPackage clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            var package = await _navigation.ShowChoosePackageAsync(instance);
            if (package == null) return;
            IsBusy = true;
            StatusText = "Applying package...";
            try
            {
                var log = await Task.Run(() => _packageService.ApplyPackage(instance, package));
                if (!string.IsNullOrEmpty(log))
                    await _navigation.ShowLogDisplayAsync(log);
            }
            catch (Exception ex) { _dialog.ShowError(ex.Message); }
            finally { IsBusy = false; StatusText = "Ready"; }
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private void ExportToCsv()
        {
            var filePath = _dialog.ShowSaveFileDialog("instances.csv", "CSV files (*.csv)|*.csv");
            if (filePath == null) return;
            _exportService.ExportCheInstancesToCsv(CheInstances.Select(r => r.Instance).ToList(), filePath);
            _dialog.ShowInfo("Export completed.");
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private void ExportToRdcMan()
        {
            var filePath = _dialog.ShowSaveFileDialog("instances.rdg", "RDCMan files (*.rdg)|*.rdg");
            if (filePath == null) return;
            _exportService.ExportToRdcManXml(CheInstances.Select(r => r.Instance).ToList(), filePath);
            _dialog.ShowInfo("Export completed.");
        }

        [RelayCommand]
        private void About() => _navigation.ShowAbout();

        [RelayCommand]
        private async Task OpenAssetLibrary() => await _navigation.ShowAssetLibrarySearchAsync();

        [RelayCommand]
        private async Task UpcomingUpdates() => await _navigation.ShowUpcomingUpdatesAsync();

        [RelayCommand]
        private async Task EnvironmentChanges()
        {
            var instance = SelectedCheInstance ?? SelectedSaasInstance;
            if (instance == null) return;
            await _navigation.ShowEnvironmentChangesAsync(instance);
        }

        [RelayCommand]
        private async Task CustomLinks() => await _navigation.ShowCustomLinksAsync();

        [RelayCommand]
        private async Task Parameters() => await _navigation.ShowParametersAsync();

        // ── Liveness ───────────────────────────────────────────────────────────

        private void FireLivenessCheck(IEnumerable<EnvironmentRow> rows)
        {
            CancelLiveness();
            _livenessCts = new CancellationTokenSource();
            var ct = _livenessCts.Token;
            _ = Task.Run(() => _livenessService.CheckAllAsync(rows, ct), ct);
        }

        private void CancelLiveness()
        {
            _livenessCts?.Cancel();
            _livenessCts?.Dispose();
            _livenessCts = null;
        }

        // ── Deployment state polling ───────────────────────────────────────────

        private static bool IsTransitionalState(DeploymentState state) =>
            state is DeploymentState.Starting or DeploymentState.Stopping;

        private static bool IsActionInProgress(LcsEnvironmentActionStatus s) =>
            s is LcsEnvironmentActionStatus.InProgress
              or LcsEnvironmentActionStatus.InProgressManually
              or LcsEnvironmentActionStatus.PreparingEnvironment;

        private void StartOrStopPolling(IReadOnlyList<EnvironmentRow> rows)
        {
            CancelPolling();
            if (!rows.Any(r => IsTransitionalState(r.Instance.DeploymentState))) return;

            _pollingCts = new CancellationTokenSource();
            _ = PollTransitionalStatesAsync(rows.ToList(), _pollingCts.Token);
        }

        private void CancelPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
        }

        /// <summary>
        /// Silently polls LCS every 30 seconds while any environment is in a
        /// transitional state (Starting/Stopping). Updates instance data in-place
        /// without showing a loading indicator. Stops automatically once all
        /// environments have left the transitional state.
        /// </summary>
        private async Task PollTransitionalStatesAsync(List<EnvironmentRow> rows, CancellationToken ct)
        {
            _logger.LogInformation("Deployment state polling started for {Count} transitional environment(s)", rows.Count);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);

                    var che = await _envService.GetCheInstancesAsync();
                    var saas = await _envService.GetSaasInstancesAsync();
                    var fresh = che.Concat(saas).ToDictionary(i => i.EnvironmentId ?? i.InstanceId ?? string.Empty);

                    bool anyStillTransitional = false;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var row in rows)
                        {
                            var key = row.Instance.EnvironmentId ?? row.Instance.InstanceId ?? string.Empty;
                            if (!fresh.TryGetValue(key, out var updated)) continue;
                            row.Instance = updated;
                            if (IsTransitionalState(updated.DeploymentState))
                                anyStillTransitional = true;
                        }
                    });

                    _logger.LogInformation("Deployment state poll complete. AnyStillTransitional={AnyStillTransitional}", anyStillTransitional);
                    if (!anyStillTransitional) break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Deployment state polling encountered an error — stopping");
            }
            finally
            {
                _logger.LogInformation("Deployment state polling stopped");
            }
        }
    }
}
