using System;
using System.Net;
using System.Net.Http;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsHttpClientService
    {
        CookieContainer CookieContainer { get; }
        HttpClient HttpClient { get; }
        string LcsProjectId { get; set; }
        ThreeLCS.ProjectType LcsProjectTypeId { get; set; }
        string LcsUrl { get; }
        string LcsUpdateUrl { get; }
        string LcsDiagUrl { get; }
        void ChangeLcsProjectId(string value);

        /// <summary>
        /// Temporarily switches <see cref="LcsProjectId"/> and <see cref="LcsProjectTypeId"/> to the
        /// specified project and returns an <see cref="IDisposable"/> that restores the original values
        /// on dispose. Use in a <c>using</c> statement to safely scope a cross-project fetch.
        /// </summary>
        IDisposable BeginProjectScope(int projectId, ProjectType projectTypeId);
    }
}
