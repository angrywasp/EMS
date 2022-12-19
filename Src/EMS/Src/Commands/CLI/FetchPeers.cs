using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Net;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("fetch_peers", "Ask your peers for more connections")]
    public class FetchPeers : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            var msg = await ExchangePeerList.GenerateRequest(true, null).ConfigureAwait(false);
            await Helpers.MessageAll(msg.ToArray()).ConfigureAwait(false);
            return true;
        }
    }
}