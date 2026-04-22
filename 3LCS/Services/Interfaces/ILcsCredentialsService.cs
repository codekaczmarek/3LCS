using System.Collections.Generic;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsCredentialsService
    {
        Dictionary<string, string>? GetCredentials(string environmentId, string itemName);
        List<RDPConnectionDetails> GetRdpConnectionDetails(CloudHostedInstance instance);
    }
}
