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
    }
}
