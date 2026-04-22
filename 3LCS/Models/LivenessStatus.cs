namespace ThreeLCS.Models
{
    public enum LivenessStatus
    {
        /// <summary>Check has not been initiated yet.</summary>
        Unknown,
        /// <summary>TCP probe is in progress.</summary>
        Checking,
        /// <summary>Port 443 (or 80) responded within the timeout.</summary>
        Alive,
        /// <summary>Neither port 443 nor 80 responded within the timeout.</summary>
        Unreachable
    }
}
