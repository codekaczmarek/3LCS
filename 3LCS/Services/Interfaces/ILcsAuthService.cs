using System.Net;
using System.Threading.Tasks;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsAuthService
    {
        CookieContainer? ExtractCookiesFromBrowser();
        bool IsLoggedIn { get; }
        void SetCookies(CookieContainer cookies);
        bool RestoreSavedCookies();
        void Logout();
    }
}
