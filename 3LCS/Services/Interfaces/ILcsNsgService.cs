using System.Threading.Tasks;
using ThreeLCS.Models;

namespace ThreeLCS.Services.Interfaces
{
    public interface ILcsNsgService
    {
        NetworkSecurityGroup? GetNetworkSecurityGroup(CloudHostedInstance instance);
        Task<bool> AddNsgRuleAsync(CloudHostedInstance instance, string ruleName, string ipOrCidr);
        Task<string> DeleteNsgRuleAsync(CloudHostedInstance instance, string rule);
    }
}
