using System.Collections.Generic;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface IFavouritesService
    {
        /// <summary>Returns the full project tree (immutable snapshot).</summary>
        IReadOnlyList<FavouriteProject> GetAll();

        #region Environments

        bool IsEnvironmentFavourite(int projectId, string environmentId);

        void AddEnvironment(int projectId, string projectName, int projectTypeId,
                            string environmentId, string environmentName);

        void RemoveEnvironment(int projectId, string environmentId);

        #endregion

        #region Projects

        bool IsProjectFavourite(int projectId);

        void AddProjectFavourite(int projectId, string projectName, int projectTypeId);

        void RemoveProjectFavourite(int projectId);

        #endregion

        #region UI state

        void SetProjectExpanded(int projectId, bool expanded);

        #endregion

        #region Friendly names

        string? GetProjectFriendlyName(int projectId);
        string? GetEnvironmentFriendlyName(int projectId, string environmentId);

        void SetProjectFriendlyName(int projectId, string projectName, int projectTypeId, string? friendlyName);

        void SetEnvironmentFriendlyName(int projectId, string projectName, int projectTypeId,
                                        string environmentId, string environmentName, string? friendlyName);

        #endregion
    }
}

