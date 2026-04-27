using System.Net;
using System.Threading.Tasks;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsAuthService
    {
        bool IsLoggedIn { get; }
        void SetCookies(CookieContainer cookies);
        bool RestoreSavedCookies();
        void Logout();

        /// <summary>
        /// Silently re-authenticates by extracting fresh cookies from the browser,
        /// applying them, and refreshing the anti-forgery token.
        /// Returns true if re-authentication succeeded (valid session restored).
        /// Concurrent callers are serialised — only one re-auth attempt runs at a time.
        /// </summary>
        Task<bool> SilentReAuthAsync();
    }
}
