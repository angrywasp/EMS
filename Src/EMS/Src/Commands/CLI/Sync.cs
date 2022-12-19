using System.Threading.Tasks;
using AngryWasp.Cli;
using EMS.Commands.P2P;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("sync", "Manually sync new messages from your connected peers")]
    public class Sync : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            await Helpers.MessageAll(RequestMessagePool.GenerateRequest(true).ToArray()).ConfigureAwait(false);
            return true;
        }
    }
}