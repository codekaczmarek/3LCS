namespace ThreeLCS.Models
{
    public enum LcsSessionState
    {
        NotLoggedIn,
        LoggedIn,
        SessionExpired,   // HTTP 498 — LCS session gone, must re-login
        TokenExpired,     // HTTP 403 — anti-forgery token stale
        TokenRefreshing,  // Currently fetching a new anti-forgery token
    }
}
