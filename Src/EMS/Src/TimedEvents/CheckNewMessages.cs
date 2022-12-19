using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS.TimedEvents
{
    [TimedEvent(60)]
    public class CheckNewMessages : ITimedEvent
    {
        public async Task Execute() => await Helpers.MessageAll(RequestMessagePool.GenerateRequest(true).ToArray()).ConfigureAwait(false);
    }
}