using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.ViewModels
{
    public partial class ChooseProjectViewModel : ObservableObject
    {
        private readonly ILcsProjectService _projectService;
        private readonly ILcsHttpClientService _http;
        private readonly IDialogService _dialog;
        private readonly IFavouritesService _favourites;

        [ObservableProperty] private ObservableCollection<LcsProject> _projects = new();
        [ObservableProperty] private LcsProject? _selectedProject;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusText = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;

        public bool IsNotBusy => !IsBusy;
        public ICollectionView FilteredProjects { get; private set; }

        partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));

        partial void OnSearchTextChanged(string value) => FilteredProjects.Refresh();

        partial void OnProjectsChanged(ObservableCollection<LcsProject> value)
        {
            FilteredProjects = CollectionViewSource.GetDefaultView(value);
            FilteredProjects.Filter = FilterProject;
            // Default sort: favourites first, then by name
            FilteredProjects.SortDescriptions.Clear();
            FilteredProjects.SortDescriptions.Add(new SortDescription(nameof(LcsProject.Favorite), ListSortDirection.Descending));
            FilteredProjects.SortDescriptions.Add(new SortDescription(nameof(LcsProject.Name), ListSortDirection.Ascending));
            OnPropertyChanged(nameof(FilteredProjects));
        }

        private bool FilterProject(object obj)
        {
            if (obj is not LcsProject project) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var q = SearchText.Trim();
            return (project.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                || (project.OrganizationName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                || (project.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                || project.Id.ToString().Contains(q);
        }

        public ChooseProjectViewModel(ILcsProjectService projectService, ILcsHttpClientService http, IDialogService dialog, IFavouritesService favourites)
        {
            _projectService = projectService;
            _http = http;
            _dialog = dialog;
            _favourites = favourites;
            // Initialise with empty view so binding doesn't fail before load
            FilteredProjects = CollectionViewSource.GetDefaultView(_projects);
        }

        [RelayCommand]
        private async Task LoadProjects()
        {
            IsBusy = true;
            StatusText = string.Empty;
            try
            {
                var projects = await _projectService.GetAllProjectsAsync();
                Projects = new ObservableCollection<LcsProject>(projects);
                if (projects.Count == 0)
                    StatusText = "No projects found. Your session may have expired — please log in again.";
            }
            catch (Exception ex)
            {
                StatusText = "Failed to load projects — your session may have expired.";
                _dialog.ShowError($"Could not load projects: {ex.Message}\n\nPlease log out and log in again.");
                foreach (Window window in Application.Current.Windows)
                    if (window.DataContext == this) { window.DialogResult = false; break; }
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void SelectProject()
        {
            if (SelectedProject == null) return;
            _http.ChangeLcsProjectId(SelectedProject.Id.ToString());
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.DialogResult = true; break; }
        }

        [RelayCommand]
        private void Cancel()
        {
            foreach (Window window in Application.Current.Windows)
                if (window.DataContext == this) { window.DialogResult = false; break; }
        }

        [RelayCommand]
        private void AddProjectToFavourites()
        {
            if (SelectedProject == null) return;
            _favourites.AddProjectFavourite(
                SelectedProject.Id,
                SelectedProject.Name ?? string.Empty,
                (int)SelectedProject.ProjectTypeId);
        }

        [RelayCommand]
        private void RemoveProjectFromFavourites()
        {
            if (SelectedProject == null) return;
            _favourites.RemoveProjectFavourite(SelectedProject.Id);
        }

        [RelayCommand]
        private void SetProjectFriendlyName()
        {
            if (SelectedProject == null) return;
            var current = _favourites.GetProjectFriendlyName(SelectedProject.Id) ?? string.Empty;
            var result = _dialog.ShowInput(
                "Friendly name (leave empty to clear):", "Set Friendly Name", current);
            if (result == null) return;
            _favourites.SetProjectFriendlyName(
                SelectedProject.Id,
                SelectedProject.Name ?? string.Empty,
                (int)SelectedProject.ProjectTypeId,
                result);
        }
    }
}
