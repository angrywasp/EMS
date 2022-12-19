using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Cli;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("prune_peers", "Ping all nodes and remove dead connections")]
    public class PrunePeerList : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            await Helpers.MessageAll(AngryWasp.Net.Ping.GenerateRequest().ToArray()).ConfigureAwait(false);
            return true;
        }
            
    }
}