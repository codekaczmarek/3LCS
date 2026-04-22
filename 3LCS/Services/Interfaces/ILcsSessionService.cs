using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    /// <summary>
    /// Central store for the current LCS API session state and the anti-forgery token.
    /// Single authority for session state and token management.
    /// </summary>
    public interface ILcsSessionService
    {
        LcsSessionState CurrentState { get; }
        bool IsLoggedIn { get; }

        /// <summary>Human-readable description of the current state, suitable for UI display.</summary>
        string StatusDescription { get; }

        /// <summary>
        /// The most recently acquired anti-forgery (__RequestVerificationToken) value.
        /// Null until the first token fetch completes.
        /// </summary>
        string? RequestVerificationToken { get; }

        /// <summary>Returns cached token if valid; fetches new one if missing. Returns null if session not authenticated.</summary>
        Task<string?> GetValidTokenAsync();

        /// <summary>Clears the cached token and immediately fetches a fresh one. Returns new token or null on failure.</summary>
        Task<string?> ForceRefreshTokenAsync();

        /// <summary>Invalidates the cached token without changing session state (called when HTTP 403 received).</summary>
        void InvalidateToken();

        /// <summary>Called by LcsAuthService when user successfully logs in (cookies set/restored).</summary>
        void NotifyLoggedIn();

        /// <summary>Called by LcsAuthService when user logs out.</summary>
        void NotifyLoggedOut();

        /// <summary>Called by LcsServiceBase when HTTP 498 (session expired) is detected.</summary>
        void NotifySessionExpired();
    }
}
