namespace ThreeLCS.Messages
{
    /// <summary>Broadcast by LcsHttpClientService whenever a new anti-forgery token is successfully fetched.</summary>
    public record TokenAcquiredMessage(string Token, string FetchedFrom);
}
