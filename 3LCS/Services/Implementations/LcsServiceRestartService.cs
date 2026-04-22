using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Services.Implementations
{
    public class LcsServiceRestartService : LcsServiceBase, ILcsServiceRestartService
    {
        public LcsServiceRestartService(ILcsHttpClientService http, ILcsSessionService sessionState, ILogger<LcsServiceRestartService> logger)
            : base(http, sessionState, logger) { }

        public System.Collections.Generic.List<ServiceToRestart>? GetServicesToRestart()
            => GetSync<System.Collections.Generic.List<ServiceToRestart>>($"{Http.LcsUrl}/EnvironmentServicingV2/GetServicesToRestart/{Http.LcsProjectId}?_={Ts()}");

        public ServiceRestartResponseData? RestartService(CloudHostedInstance instance, string serviceToRestart)
            => PostFormSync<ServiceRestartResponseData>(
                $"{Http.LcsUrl}/EnvironmentServicingV2/RestartService/{Http.LcsProjectId}",
                $"lcsEnvironmentId={instance.EnvironmentId}&axServiceName={serviceToRestart}");
    }
}
