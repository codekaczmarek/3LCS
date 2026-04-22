using System.Collections.Generic;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsServiceRestartService
    {
        List<ServiceToRestart>? GetServicesToRestart();
        ServiceRestartResponseData? RestartService(CloudHostedInstance instance, string serviceToRestart);
    }
}
