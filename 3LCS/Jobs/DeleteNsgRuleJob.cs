using System.Threading.Tasks;
using ThreeLCS.Models;
using ThreeLCS.Services.Interfaces;

namespace ThreeLCS.Jobs
{
    public sealed class DeleteNsgRuleJob : BackgroundJob
    {
        private readonly ILcsNsgService _nsgService;
        private readonly CloudHostedInstance _instance;
        private readonly NSGRule _rule;

        public override string Name => "Delete NSG Rule";

        /// <summary>
        /// The result message returned by LCS after a successful deletion.
        /// Null until <see cref="ExecuteAsync"/> completes without throwing.
        /// </summary>
        public string? ResultMessage { get; private set; }

        public DeleteNsgRuleJob(ILcsNsgService nsgService, CloudHostedInstance instance, NSGRule rule)
        {
            _nsgService = nsgService;
            _instance = instance;
            _rule = rule;
        }

        public override async Task ExecuteAsync(IJobContext context)
            => ResultMessage = await _nsgService.DeleteNsgRuleAsync(_instance, _rule.Name ?? string.Empty);
    }
}
