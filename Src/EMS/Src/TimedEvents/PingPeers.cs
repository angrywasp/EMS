using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60)]
    public class PingPeers : ITimedEvent
    {
        public async Task Execute() => await Helpers.MessageAll(AngryWasp.Net.Ping.GenerateRequest().ToArray()).ConfigureAwait(false);    
    }
}