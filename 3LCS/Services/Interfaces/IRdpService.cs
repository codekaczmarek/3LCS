using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface IRdpService
    {
        Task ConnectAsync(CloudHostedInstance instance, List<RDPConnectionDetails> rdpList);
        RDPConnectionDetails? ChooseUser(List<RDPConnectionDetails> rdpList);
    }
}
