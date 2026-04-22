using System;

namespace ThreeLCS.Messages
{
    public record ApiCallCompletedMessage(Guid Id, int? StatusCode, long DurationMs, string? Error);
}
