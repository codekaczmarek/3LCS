using System;

namespace ThreeLCS.Messages
{
    public record ApiCallStartedMessage(Guid Id, string Method, string Url, DateTime StartedAt);
}
