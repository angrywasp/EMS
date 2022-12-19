using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60)]
    public class ExchangePeerLists : ITimedEvent
    {
        public async Task Execute()
        {
            var req = await AngryWasp.Net.ExchangePeerList.GenerateRequest(true, null).ConfigureAwait(false);
            await Helpers.MessageAll(req.ToArray()).ConfigureAwait(false);
        }
    }
}