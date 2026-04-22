using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LivenessService : ILivenessService
    {
        private const int TimeoutMs = 4000;
        private readonly ILogger<LivenessService> _logger;

        public LivenessService(ILogger<LivenessService> logger) => _logger = logger;

        public Task CheckAllAsync(IEnumerable<EnvironmentRow> rows, CancellationToken ct = default)
            => Task.WhenAll(rows.Select(row => CheckOneAsync(row, ct)));

        private async Task CheckOneAsync(EnvironmentRow row, CancellationToken ct)
        {
            var host = GetHost(row.Instance);
            if (host == null)
            {
                row.Liveness = LivenessStatus.Unknown;
                return;
            }

            _logger.LogDebug("Liveness check started for {Host}", host);

            var result = (await TryTcpAsync(host, 443, ct) || await TryTcpAsync(host, 80, ct))
                ? LivenessStatus.Alive
                : LivenessStatus.Unreachable;

            row.Liveness = result;
            _logger.LogDebug("Liveness check {Result} for {Host}", result, host);
        }

        private static async Task<bool> TryTcpAsync(string host, int port, CancellationToken outerCt)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
            cts.CancelAfter(TimeoutMs);
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port, cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? GetHost(CloudHostedInstance instance)
        {
            var link = instance.NavigationLinks?
                .FirstOrDefault(l => l.DisplayName == "Log on to environment");
            if (link?.NavigationUri == null) return null;
            return Uri.TryCreate(link.NavigationUri, UriKind.Absolute, out var uri) ? uri.Host : null;
        }
    }
}
