using ThreeLCS.Models;

namespace ThreeLCS.Messages
{
    /// <summary>Broadcast whenever the LCS session state changes (login, logout, expiry, token refresh).</summary>
    public record SessionStateChangedMessage(LcsSessionState NewState, string? Detail = null);
}
