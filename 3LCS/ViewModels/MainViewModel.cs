using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeLCS.Commands;
using ThreeLCS.Jobs;
using ThreeLCS.Messages;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<SessionStateChangedMessage>, IBusyHost, IDeploymentCoordinator
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
        private readonly ILcsAuthService _authService;
        private readonly ILcsSessionService _sessionState;
        private readonly ILivenessService _livenessService;
        private readonly IBackgroundTaskService _taskService;
        private readonly IBackgroundJobRunner _runner;
        private readonly IFavouritesService _favourites;
        private readonly ILogger<MainViewModel> _logger;
        private readonly CloudHostedInstanceLogonCommand _cloudHostedInstanceLogonCommand;
        private readonly CloudHostedInstanceOpenDetailsCommand _cloudHostedInstanceOpenDetailsCommand;
        private readonly CloudHostedInstanceOpenMonitoringCommand _cloudHostedInstanceOpenMonitoringCommand;
        private readonly CloudHostedInstanceOpenDetailedVersionInfoCommand _cloudHostedInstanceOpenDetailedVersionInfoCommand;
        private readonly CloudHostedInstanceOpenChangeHistoryCommand _cloudHostedInstanceOpenChangeHistoryCommand;
        private readonly CloudHostedInstanceOpenEnvironmentChangesCommand _cloudHostedInstanceOpenEnvironmentChangesCommand;
        private readonly CloudHostedInstanceOpenBuildInfoCommand _cloudHostedInstanceOpenBuildInfoCommand;
        private readonly CloudHostedInstanceDeleteCommand _cloudHostedInstanceDeleteCommand;
        private readonly CloudHostedInstanceStartCommand _cloudHostedInstanceStartCommand;
        private readonly CloudHostedInstanceStopCommand _cloudHostedInstanceStopCommand;
        private readonly CloudHostedInstanceApplyPackageCommand _cloudHostedInstanceApplyPackageCommand;
        private const string AutoRefreshKey     = "AutoRefresh";
        private const string LivenessKey         = "Liveness";
        private const string DeploymentPollingKey = "DeploymentPolling";

        private bool _autoLoginInProgress;
        private readonly Dictionary<string, LivenessStatus> _livenessCache = new();

        public ILcsApiMonitorService ApiMonitor { get; }
        public IBackgroundTaskService TaskService { get; }

        [ObservableProperty] private ObservableCollection<EnvironmentViewModel> _cheInstances = new();
        [ObservableProperty] private ObservableCollection<EnvironmentViewModel> _saasInstances = new();
        [ObservableProperty] private ObservableCollection<EnvironmentViewModel> _selectedCheRows = new();
        [ObservableProperty] private ObservableCollection<EnvironmentViewModel> _selectedSaasRows = new();
        [ObservableProperty] private ObservableCollection<FavouriteProjectViewModel> _favouriteGroups = new();
        [ObservableProperty] private EnvironmentViewModel? _selectedCheRow;
        [ObservableProperty] private EnvironmentViewModel? _selectedSaasRow;
        [ObservableProperty] private EnvironmentViewModel? _selectedFavouriteRow;
        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = "Not logged in. Use File → Login to LCS.";
        [ObservableProperty] private LcsProject? _selectedProject;
        [ObservableProperty] private string _windowTitle = "3LCS";
        [ObservableProperty] private bool _isLoggedIn;
        [ObservableProperty] private bool _isMonitorOpen;

        // Active environment row: favour the selected favourite tile when on the Favourites tab (index 0)
        private EnvironmentViewModel? ActiveCheRow => SelectedTabIndex == 0 ? SelectedFavouriteRow : SelectedCheRow;
        private CloudHostedInstance? ActiveCheInstance => ActiveCheRow?.Instance;
        private CloudHostedInstance? SelectedSaasInstance => SelectedSaasRow?.Instance;
        private CloudHostedInstance? ActiveEnvInstance => ActiveCheInstance ?? SelectedSaasInstance;

        // False when the Microsoft-Managed Environments tab is active (tab index 2);
        // used as CanExecute guard for destructive/deployment commands that must not act on SaaS environments.
        private bool IsLoggedInAndCheActive => IsLoggedIn && SelectedTabIndex != 2;

        public void SetSelectedCheRows(IEnumerable<EnvironmentViewModel> rows)
        {
            SelectedCheRows = new ObservableCollection<EnvironmentViewModel>(rows);
            NotifyEnvironmentSelectionCommands();
        }

        public void SetSelectedSaasRows(IEnumerable<EnvironmentViewModel> rows)
        {
            SelectedSaasRows = new ObservableCollection<EnvironmentViewModel>(rows);
            NotifyEnvironmentSelectionCommands();
        }

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
            IBackgroundTaskService taskService,
            IBackgroundJobRunner runner,
            IFavouritesService favourites,
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
            ApiMonitor = apiMonitor;
            _authService = authService;
            _sessionState = sessionState;
            _livenessService = livenessService;
            _taskService = taskService;
            _runner = runner;
            _favourites = favourites;
            TaskService = taskService;
            _logger = logger;
            _cloudHostedInstanceLogonCommand = new CloudHostedInstanceLogonCommand(_dialog, _logger);
            _cloudHostedInstanceOpenDetailsCommand = new CloudHostedInstanceOpenDetailsCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceOpenMonitoringCommand = new CloudHostedInstanceOpenMonitoringCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceOpenDetailedVersionInfoCommand = new CloudHostedInstanceOpenDetailedVersionInfoCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceOpenChangeHistoryCommand = new CloudHostedInstanceOpenChangeHistoryCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceOpenEnvironmentChangesCommand = new CloudHostedInstanceOpenEnvironmentChangesCommand(_navigation, _dialog, _logger);
            _cloudHostedInstanceOpenBuildInfoCommand = new CloudHostedInstanceOpenBuildInfoCommand(_navigation, _dialog, _logger);
            _cloudHostedInstanceDeleteCommand = new CloudHostedInstanceDeleteCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceStartCommand = new CloudHostedInstanceStartCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceStopCommand = new CloudHostedInstanceStopCommand(_envService, _dialog, _logger);
            _cloudHostedInstanceApplyPackageCommand = new CloudHostedInstanceApplyPackageCommand(packageService, _navigation, _dialog, _logger);
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
            OpenFavouriteProjectCommand.NotifyCanExecuteChanged();
            MarkProjectFavouriteCommand.NotifyCanExecuteChanged();

            if (value) StartAutoRefresh();
            else StopAutoRefresh();
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            // Re-evaluate commands that are blocked on the Microsoft-Managed Environments tab
            StartEnvironmentCommand.NotifyCanExecuteChanged();
            StopEnvironmentCommand.NotifyCanExecuteChanged();
            DeleteEnvironmentCommand.NotifyCanExecuteChanged();
            ApplyPackageCommand.NotifyCanExecuteChanged();
        }

        private void NotifyEnvironmentSelectionCommands()
        {
            OpenRdpCommand.NotifyCanExecuteChanged();
            DeleteEnvironmentCommand.NotifyCanExecuteChanged();
            StartEnvironmentCommand.NotifyCanExecuteChanged();
            StopEnvironmentCommand.NotifyCanExecuteChanged();
            AddNsgRuleCommand.NotifyCanExecuteChanged();
            DeleteNsgRuleCommand.NotifyCanExecuteChanged();
            ApplyPackageCommand.NotifyCanExecuteChanged();
        }

        #region Auth

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

        #endregion

        #region Commands

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
            // Do NOT cancel polling here — a force-polling loop started by a recent Start/Stop
            // request must survive the refresh so RDP windows keep receiving state updates.
            try
            {
                var che = await _envService.GetCheInstancesAsync();
                var saas = await _envService.GetSaasInstancesAsync();

                // Snapshot current liveness before rebuilding rows (needed for new environments).
                foreach (var r in CheInstances.Concat(SaasInstances))
                {
                    var key = r.Instance.EnvironmentId ?? r.Instance.InstanceId;
                    if (key != null) _livenessCache[key] = r.Liveness;
                }

                // Reuse existing EnvironmentViewModel objects where possible — updating Instance
                // in-place preserves live references held by any open RDP windows, so their
                // PropertyChanged subscriptions keep firing correctly on the same object.
                var existingChe  = CheInstances .ToDictionary(r => r.Instance.EnvironmentId ?? r.Instance.InstanceId ?? string.Empty);
                var existingSaas = SaasInstances.ToDictionary(r => r.Instance.EnvironmentId ?? r.Instance.InstanceId ?? string.Empty);

                EnvironmentViewModel ReuseOrCreate(CloudHostedInstance i, Dictionary<string, EnvironmentViewModel> existing)
                {
                    var key = i.EnvironmentId ?? i.InstanceId ?? string.Empty;
                    if (existing.TryGetValue(key, out var vm))
                    {
                        vm.Instance = i;   // update in-place; PropertyChanged fires on the same object
                        vm.FriendlyName = key != string.Empty
                            ? _favourites.GetEnvironmentFriendlyName(SelectedProject?.Id ?? 0, key)
                            : null;
                        return vm;
                    }
                    var fresh = new EnvironmentViewModel(i);
                    if (key != string.Empty && _livenessCache.TryGetValue(key, out var cached))
                        fresh.Liveness = cached;
                    fresh.FriendlyName = key != string.Empty
                        ? _favourites.GetEnvironmentFriendlyName(SelectedProject?.Id ?? 0, key)
                        : null;
                    return fresh;
                }

                var cheRows  = che .Select(i => ReuseOrCreate(i, existingChe )).ToList();
                var saasRows = saas.Select(i => ReuseOrCreate(i, existingSaas)).ToList();

                CheInstances = new ObservableCollection<EnvironmentViewModel>(cheRows);
                SaasInstances = new ObservableCollection<EnvironmentViewModel>(saasRows);
                StatusText = $"Loaded {CheInstances.Count} CHE and {SaasInstances.Count} SaaS instances.";

                var allRows = cheRows.Concat(saasRows).ToList();
                if (_settings.LivenessCheckEnabled)
                    FireLivenessCheck(allRows);
                StartOrStopPolling(allRows);
                await RefreshFavouritesAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                _dialog.ShowError(ex.Message);
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

        #region Project quick-search

        private List<LcsProject>? _cachedProjects;

        [ObservableProperty] private string _projectSearchText = string.Empty;
        [ObservableProperty] private ObservableCollection<LcsProject> _projectSuggestions = new();
        [ObservableProperty] private bool _isProjectPickerOpen;
        [ObservableProperty] private LcsProject? _selectedProjectSuggestion;

        partial void OnSelectedProjectSuggestionChanged(LcsProject? value)
        {
            if (value == null) return;
            QuickSwitchProject(value);
        }

        partial void OnProjectSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsProjectPickerOpen = false;
                ProjectSuggestions.Clear();
                return;
            }
            if (_cachedProjects == null)
                _ = EnsureProjectCacheAsync();
            else
                FilterProjectSuggestions(value);
        }

        private async Task EnsureProjectCacheAsync()
        {
            try { _cachedProjects = await _projectService.GetAllProjectsAsync(); }
            catch { _cachedProjects = new List<LcsProject>(); }
            FilterProjectSuggestions(ProjectSearchText);
        }

        private void FilterProjectSuggestions(string query)
        {
            ProjectSuggestions.Clear();
            if (_cachedProjects == null) return;
            foreach (var p in _cachedProjects)
            {
                if (p.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    p.OrganizationName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    p.Id.ToString().Contains(query))
                    ProjectSuggestions.Add(p);
            }
            IsProjectPickerOpen = ProjectSuggestions.Count > 0;
        }

        [RelayCommand]
        private void CloseProjectPicker()
        {
            IsProjectPickerOpen = false;
            ProjectSearchText = string.Empty;
        }

        private void QuickSwitchProject(LcsProject project)
        {
            IsProjectPickerOpen = false;
            ProjectSearchText = string.Empty;
            SelectedProjectSuggestion = null;
            SelectedProject = project;
            _http.ChangeLcsProjectId(project.Id.ToString());
            _http.LcsProjectTypeId = project.ProjectTypeId;
            WindowTitle = $"3LCS — {project.Name}";
            _settings.LastProjectId = project.Id.ToString();
            _settings.LastProjectName = project.Name ?? string.Empty;
            _settings.LastProjectTypeId = (int)project.ProjectTypeId;
            _settings.Save();
            _ = Refresh();
        }

        #endregion


        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private void OpenRdp()
        {
            var env = ActiveCheRow;
            _logger.LogDebug("OpenRdp clicked. SelectedCheInstance={Instance}", env?.Instance?.DisplayName ?? "<null>");
            if (env == null) return;
            using var scope = EnvProjectScope(env);
            _ = _navigation.ShowRdpLaunchAsync(env);
        }

        [RelayCommand]
        private async Task ShowBackgroundTasks() => await _navigation.ShowBackgroundTasksAsync();

        [RelayCommand]
        private void LogonToApplication() => _cloudHostedInstanceLogonCommand.Execute(CreateEnvironmentCommandContext());

        [RelayCommand]
        private void OpenInstanceDetails() => _cloudHostedInstanceOpenDetailsCommand.Execute(CreateEnvironmentCommandContext());

        [RelayCommand]
        private void OpenEnvironmentMonitoring() => _cloudHostedInstanceOpenMonitoringCommand.Execute(CreateEnvironmentCommandContext());

        [RelayCommand]
        private void OpenDetailedVersionInfo() => _cloudHostedInstanceOpenDetailedVersionInfoCommand.Execute(CreateEnvironmentCommandContext());

        [RelayCommand]
        private void OpenEnvironmentChangeHistory() => _cloudHostedInstanceOpenChangeHistoryCommand.Execute(CreateEnvironmentCommandContext());

        [RelayCommand]
        private async Task OpenEnvironmentChanges() => await _cloudHostedInstanceOpenEnvironmentChangesCommand.ExecuteAsync(CreateEnvironmentCommandContext());

        [RelayCommand]
        private async Task OpenBuildInfo() => await _cloudHostedInstanceOpenBuildInfoCommand.ExecuteAsync(CreateEnvironmentCommandContext());

        [RelayCommand(CanExecute = nameof(IsLoggedInAndCheActive))]
        private async Task DeleteEnvironment() => await _cloudHostedInstanceDeleteCommand.ExecuteAsync(CreateEnvironmentCommandContext());

        [RelayCommand(CanExecute = nameof(IsLoggedInAndCheActive))]
        private async Task StartEnvironment() => await _cloudHostedInstanceStartCommand.ExecuteAsync(CreateEnvironmentCommandContext());

        [RelayCommand(CanExecute = nameof(IsLoggedInAndCheActive))]
        private async Task StopEnvironment() => await _cloudHostedInstanceStopCommand.ExecuteAsync(CreateEnvironmentCommandContext());

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task AddNsgRule()
        {
            var row = ActiveCheRow ?? SelectedSaasRow;
            var instance = ActiveEnvInstance;
            _logger.LogDebug("AddNsgRule clicked. Instance={Instance}", instance?.DisplayName ?? "<null>");
            if (instance == null) return;
            using var scope = EnvProjectScope(row);
            await _navigation.ShowAddNsgRuleAsync(instance);
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task DeleteNsgRule()
        {
            var row = ActiveCheRow ?? SelectedSaasRow;
            var instance = ActiveEnvInstance;
            if (instance == null) return;
            var rule = await _navigation.ShowChooseNsgAsync(instance);
            if (rule == null) return;
            var job = new DeleteNsgRuleJob(_nsgService, instance, rule);
            using (EnvProjectScope(row))
                if (await _runner.RunAsCommandAsync(job, this, _dialog))
                    _dialog.ShowInfo(string.IsNullOrEmpty(job.ResultMessage) ? "NSG rule deleted." : job.ResultMessage);
        }

        [RelayCommand(CanExecute = nameof(IsLoggedInAndCheActive))]
        private async Task ApplyPackage() => await _cloudHostedInstanceApplyPackageCommand.ExecuteAsync(CreateEnvironmentCommandContext());

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
        private async Task EnvironmentChanges() => await OpenEnvironmentChanges();

        [RelayCommand]
        private async Task CustomLinks() => await _navigation.ShowCustomLinksAsync();

        [RelayCommand]
        private async Task Parameters() => await _navigation.ShowParametersAsync();

        #endregion

        #region Favourites

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private void AddToFavourites()
        {
            var row = SelectedCheRow;
            var instance = row?.Instance;
            if (instance == null || SelectedProject == null) return;
            _favourites.AddEnvironment(
                SelectedProject.Id,
                SelectedProject.Name ?? string.Empty,
                (int)SelectedProject.ProjectTypeId,
                instance.EnvironmentId ?? string.Empty,
                instance.DisplayName ?? string.Empty);
            _ = RefreshFavouritesAsync();
        }

        [RelayCommand]
        private async Task RemoveFromFavourites()
        {
            var row = SelectedFavouriteRow;
            if (row?.ProjectId == null || row.Instance.EnvironmentId == null) return;
            _favourites.RemoveEnvironment(row.ProjectId.Value, row.Instance.EnvironmentId);
            SelectedFavouriteRow = null;
            await RefreshFavouritesAsync();
        }

        [RelayCommand(CanExecute = nameof(IsLoggedIn))]
        private async Task OpenFavouriteProject(FavouriteProjectViewModel? group)
        {
            if (group == null) return;

            SelectedProject = new LcsProject
            {
                Id = group.ProjectId,
                Name = group.ProjectName,
                ProjectTypeId = (ProjectType)group.ProjectTypeId
            };
            _http.ChangeLcsProjectId(group.ProjectId.ToString());
            _http.LcsProjectTypeId = (ProjectType)group.ProjectTypeId;
            WindowTitle = $"3LCS — {group.ProjectName}";
            _settings.LastProjectId = group.ProjectId.ToString();
            _settings.LastProjectName = group.ProjectName;
            _settings.LastProjectTypeId = group.ProjectTypeId;
            _settings.Save();

            SelectedTabIndex = 1;
            await Refresh();
        }

        [RelayCommand]
        private async Task MarkProjectFavourite(FavouriteProjectViewModel? group)
        {
            if (group == null) return;
            _favourites.AddProjectFavourite(group.ProjectId, group.ProjectName, group.ProjectTypeId);
            await RefreshFavouritesAsync();
        }

        [RelayCommand]
        private async Task UnmarkProjectFavourite(FavouriteProjectViewModel? group)
        {
            if (group == null) return;
            _favourites.RemoveProjectFavourite(group.ProjectId);
            await RefreshFavouritesAsync();
        }

        [RelayCommand]
        private void SetProjectFriendlyName(FavouriteProjectViewModel? group)
        {
            if (group == null) return;
            var result = _dialog.ShowInput(
                "Friendly name (leave empty to clear):", "Set Friendly Name", group.FriendlyName ?? string.Empty);
            if (result == null) return;
            var alias = string.IsNullOrWhiteSpace(result) ? null : result.Trim();
            group.FriendlyName = alias;
            _favourites.SetProjectFriendlyName(group.ProjectId, group.ProjectName, group.ProjectTypeId, alias);
        }

        [RelayCommand]
        private async Task SetEnvironmentFriendlyName()
        {
            var row = ActiveCheRow;
            if (row?.Instance.EnvironmentId == null) return;

            var result = _dialog.ShowInput(
                "Friendly name (leave empty to clear):", "Set Friendly Name", row.FriendlyName ?? string.Empty);
            if (result == null) return;

            var alias = string.IsNullOrWhiteSpace(result) ? null : result.Trim();

            int projId;
            string projName;
            int projTypeId;
            if (row.ProjectId.HasValue)
            {
                projId    = row.ProjectId.Value;
                projName  = row.ProjectName ?? string.Empty;
                projTypeId = row.ProjectTypeId ?? 0;
            }
            else if (SelectedProject != null)
            {
                projId    = SelectedProject.Id;
                projName  = SelectedProject.Name ?? string.Empty;
                projTypeId = (int)SelectedProject.ProjectTypeId;
            }
            else return;

            var wasFav = _favourites.IsEnvironmentFavourite(projId, row.Instance.EnvironmentId);
            _favourites.SetEnvironmentFriendlyName(
                projId, projName, projTypeId,
                row.Instance.EnvironmentId, row.Instance.DisplayName ?? string.Empty, alias);

            // Update all VMs that display this environment
            row.FriendlyName = alias;
            var favVm = FavouriteGroups
                .SelectMany(g => g.Environments)
                .FirstOrDefault(e => e.Instance.EnvironmentId == row.Instance.EnvironmentId);
            if (favVm != null) favVm.FriendlyName = alias;

            // If the env was newly auto-added to favourites, rebuild the accordion
            if (!wasFav)
                await RefreshFavouritesAsync();
        }

        private EnvironmentCommandContext CreateEnvironmentCommandContext() => new()
        {
            BusyHost = this,
            SelectedTabIndex = SelectedTabIndex,
            ActiveCheRow = ActiveCheRow,
            SelectedCheRow = SelectedCheRow,
            SelectedSaasRow = SelectedSaasRow,
            SelectedCheRows = SelectedCheRows.ToList(),
            SelectedSaasRows = SelectedSaasRows.ToList(),
            BeginProjectScope = EnvProjectScope,
            RefreshAsync = Refresh
        };

        private async Task RefreshFavouritesAsync()
        {
            var projects = _favourites.GetAll();

            if (projects.Count == 0)
            {
                FavouriteGroups = new ObservableCollection<FavouriteProjectViewModel>();
                return;
            }

            try
            {
                // Build lookup of existing VMs keyed by (projectId, environmentId) to preserve liveness
                var existingVms = FavouriteGroups
                    .SelectMany(g => g.Environments.Select(e => (g.ProjectId, e)))
                    .ToDictionary(x => (x.ProjectId, x.e.Instance.EnvironmentId ?? string.Empty), x => x.e);

                var instancesByProject = await _envService.GetFavouriteCheInstancesAsync(projects);

                var newGroups = new List<FavouriteProjectViewModel>();

                foreach (var project in projects.OrderBy(p => p.Name))
                {
                    var group = new FavouriteProjectViewModel(
                        projectId: project.Id,
                        projectName: project.Name,
                        projectTypeId: project.ProjectTypeId,
                        isExplicitFavourite: project.Favourite,
                        isExpanded: project.Expanded,
                        friendlyName: project.FriendlyName,
                        onExpandedChanged: expanded => _favourites.SetProjectExpanded(project.Id, expanded));

                    if (instancesByProject.TryGetValue(project.Id, out var pairs))
                    {
                        foreach (var (instance, fav) in pairs)
                        {
                            var key = (project.Id, instance.EnvironmentId ?? string.Empty);
                            EnvironmentViewModel vm;
                            if (existingVms.TryGetValue(key, out var cached))
                            {
                                cached.Instance = instance;
                                vm = cached;
                            }
                            else
                            {
                                vm = new EnvironmentViewModel(instance);
                                if (_livenessCache.TryGetValue(instance.EnvironmentId ?? string.Empty, out var liveness))
                                    vm.Liveness = liveness;
                            }
                            vm.ProjectId = project.Id;
                            vm.ProjectName = project.Name;
                            vm.ProjectTypeId = project.ProjectTypeId;
                            vm.FriendlyName = fav.FriendlyName;
                            group.Environments.Add(vm);
                        }
                    }

                    newGroups.Add(group);
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FavouriteGroups = new ObservableCollection<FavouriteProjectViewModel>(newGroups);
                });

                // Trigger liveness checks across all grouped environments
                var allEnvs = newGroups.SelectMany(g => g.Environments).ToList();
                if (allEnvs.Count > 0)
                    FireLivenessCheck(allEnvs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh favourite environments");
            }
        }

        /// <summary>
        /// Returns a disposable that temporarily switches the HTTP client's project context
        /// to match the project associated with <paramref name="row"/> (when it is a favourite
        /// from a different project). Returns null if the row has no project override.
        /// </summary>
        private IDisposable? EnvProjectScope(EnvironmentViewModel? row)
        {
            if (row?.ProjectId is not { } projId || projId == 0) return null;
            return _http.BeginProjectScope(projId, (ProjectType)(row.ProjectTypeId ?? 0));
        }

        #endregion

        #region Auto-refresh

        private void StartAutoRefresh()
        {
            _runner.CancelExclusive(AutoRefreshKey);
            if (!_settings.AutoRefresh) return;
            _runner.RunExclusive(
                new AutoRefreshJob(
                    _logger,
                    canRefresh: () => IsLoggedIn && SelectedProject != null && !IsBusy,
                    refresh: Refresh),
                AutoRefreshKey);
        }

        private void StopAutoRefresh() => _runner.CancelExclusive(AutoRefreshKey);

        #endregion

        #region Liveness

        private void FireLivenessCheck(IEnumerable<EnvironmentViewModel> rows) =>
            _runner.RunExclusive(new LivenessCheckJob(_livenessService, rows), LivenessKey);

        private void CancelLiveness() => _runner.CancelExclusive(LivenessKey);

        #endregion

        #region Deployment state polling

        private void StartOrStopPolling(IReadOnlyList<EnvironmentViewModel> rows)
        {
            // Preserve an already-running polling loop (e.g. force-polling started after a
            // Start/Stop request). Only create a new loop when none is active.
            var existing = _runner.GetExclusive(DeploymentPollingKey);
            if (existing?.Status == ManagedTaskStatus.Running) return;
            if (!rows.Any(r => DeploymentPollingJob.IsTransitionalState(r.Instance.DeploymentState))) return;
            _runner.RunExclusive(
                new DeploymentPollingJob(_envService, _logger, rows.ToList()),
                DeploymentPollingKey);
        }

        /// <summary>
        /// Sends a start request for the given CHE and forces a fresh deployment polling loop.
        /// The start call is registered as a named background task visible in BackgroundTasksWindow.
        /// </summary>
        public void StartCheEnv(EnvironmentViewModel env)
        {
            _taskService.Run($"Start: {env.Instance.DisplayName}", async ct =>
            {
                bool ok = await _envService.StartStopDeploymentAsync(env.Instance, "start");
                if (!ok) throw new InvalidOperationException("LCS rejected the start request.");
            });
            ForceDeploymentPolling();
        }

        /// <summary>
        /// Starts deployment polling only if not already running. Safe to call even if polling
        /// is active — in that case it does nothing. Used by RdpLaunchViewModel.
        /// </summary>
        public void EnsureDeploymentPolling()
        {
            var existing = _runner.GetExclusive(DeploymentPollingKey);
            if (existing?.Status == ManagedTaskStatus.Running) return;
            ForceDeploymentPolling();
        }

        /// <summary>
        /// Checks current states and starts polling if any environment is transitional.
        /// Called after Refresh() when force-polling was not already active.
        /// </summary>
        public void TriggerDeploymentPolling()
        {
            var all = CheInstances.Concat(SaasInstances).ToList();
            StartOrStopPolling(all);
        }

        /// <summary>
        /// Unconditionally restarts the deployment poll loop with a minimum-iterations grace
        /// period. Use immediately after sending a Start/Stop request, before LCS reflects
        /// the new transitional state.
        /// </summary>
        public void ForceDeploymentPolling()
        {
            var all = CheInstances.Concat(SaasInstances).ToList();
            if (all.Count == 0) return;
            _runner.RunExclusive(
                new DeploymentPollingJob(_envService, _logger, all, minIterations: 5),
                DeploymentPollingKey);
        }

        private void CancelPolling() => _runner.CancelExclusive(DeploymentPollingKey);

        #endregion
    }
}
