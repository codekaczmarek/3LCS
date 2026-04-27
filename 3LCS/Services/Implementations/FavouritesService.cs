using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    /// <summary>
    /// Persists favourite projects (each owning its environments) in a single JSON file.
    /// Schema: <c>{ "Projects": [ { "Id", "Name", "ProjectTypeId", "Favourite", "Expanded",
    ///          "Environments": [ { "Id", "Name" } ] } ] }</c>
    /// </summary>
    public class FavouritesService : IFavouritesService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "3LCS", "favourites.json");

        private readonly List<FavouriteProject> _projects;

        public FavouritesService()
        {
            _projects = LoadFromDisk();
        }

        #region Persistence

        private static List<FavouriteProject> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new();

                var text = File.ReadAllText(FilePath);
                var wrapper = JsonConvert.DeserializeObject<FavouritesFile>(text);
                return wrapper?.Projects ?? new();
            }
            catch
            {
                return new();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath,
                    JsonConvert.SerializeObject(new FavouritesFile { Projects = _projects }, Formatting.Indented));
            }
            catch { }
        }

        #endregion

        #region Tree helpers

        private FavouriteProject EnsureProject(int id, string name, int projectTypeId)
        {
            var existing = _projects.FirstOrDefault(p => p.Id == id);
            if (existing != null) return existing;

            var project = new FavouriteProject
            {
                Id = id,
                Name = name,
                ProjectTypeId = projectTypeId,
                Favourite = false,
                Expanded = true
            };
            _projects.Add(project);
            return project;
        }

        /// <summary>
        /// Removes a project from the tree if it carries no inherent value:
        /// not explicitly favourited, has no favourite environments, and has no friendly name.
        /// </summary>
        private void Cleanup(FavouriteProject project)
        {
            if (!project.Favourite && project.Environments.Count == 0 && project.FriendlyName == null)
                _projects.Remove(project);
        }

        #endregion

        #region Read

        public IReadOnlyList<FavouriteProject> GetAll() => _projects.AsReadOnly();

        public bool IsEnvironmentFavourite(int projectId, string environmentId) =>
            _projects.FirstOrDefault(p => p.Id == projectId)
                     ?.Environments.Any(e => e.Id == environmentId) == true;

        public bool IsProjectFavourite(int projectId) =>
            _projects.Any(p => p.Id == projectId && p.Favourite);

        #endregion

        #region Environments

        public void AddEnvironment(int projectId, string projectName, int projectTypeId,
                                   string environmentId, string environmentName)
        {
            var project = EnsureProject(projectId, projectName, projectTypeId);
            if (project.Environments.Any(e => e.Id == environmentId)) return;
            project.Environments.Add(new FavouriteEnvironment { Id = environmentId, Name = environmentName });
            Save();
        }

        public void RemoveEnvironment(int projectId, string environmentId)
        {
            var project = _projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return;

            var env = project.Environments.FirstOrDefault(e => e.Id == environmentId);
            if (env == null) return;

            project.Environments.Remove(env);
            Cleanup(project);
            Save();
        }

        #endregion

        #region Projects

        public void AddProjectFavourite(int projectId, string projectName, int projectTypeId)
        {
            var project = EnsureProject(projectId, projectName, projectTypeId);
            if (project.Favourite) return;
            project.Favourite = true;
            Save();
        }

        public void RemoveProjectFavourite(int projectId)
        {
            var project = _projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return;
            project.Favourite = false;
            Cleanup(project);
            Save();
        }

        #endregion

        #region UI state

        public void SetProjectExpanded(int projectId, bool expanded)
        {
            var project = _projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || project.Expanded == expanded) return;
            project.Expanded = expanded;
            Save();
        }

        #endregion

        #region Friendly names

        public string? GetProjectFriendlyName(int projectId) =>
            _projects.FirstOrDefault(p => p.Id == projectId)?.FriendlyName;

        public string? GetEnvironmentFriendlyName(int projectId, string environmentId) =>
            _projects.FirstOrDefault(p => p.Id == projectId)
                     ?.Environments.FirstOrDefault(e => e.Id == environmentId)?.FriendlyName;

        public void SetProjectFriendlyName(int projectId, string projectName, int projectTypeId, string? friendlyName)
        {
            var alias = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName.Trim();
            var project = EnsureProject(projectId, projectName, projectTypeId);
            project.FriendlyName = alias;
            Cleanup(project);
            Save();
        }

        public void SetEnvironmentFriendlyName(int projectId, string projectName, int projectTypeId,
                                               string environmentId, string environmentName, string? friendlyName)
        {
            var alias = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName.Trim();

            // Find the env in any project (env IDs are globally unique GUIDs)
            var project = _projects.FirstOrDefault(p => p.Environments.Any(e => e.Id == environmentId));
            if (project == null)
            {
                // Not favourited yet — auto-create entry so the alias is persisted
                project = EnsureProject(projectId, projectName, projectTypeId);
                project.Environments.Add(new FavouriteEnvironment
                    { Id = environmentId, Name = environmentName, FriendlyName = alias });
            }
            else
            {
                var env = project.Environments.First(e => e.Id == environmentId);
                env.FriendlyName = alias;
                Cleanup(project);
            }
            Save();
        }

        #endregion

        #region Private DTO

        private sealed class FavouritesFile
        {
            public List<FavouriteProject> Projects { get; set; } = new();
        }

        #endregion
    }
}
