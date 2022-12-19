using System.Threading.Tasks;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(30)]
    public class ConnectToSeedNodes : ITimedEvent
    {
        public async Task Execute()
        {
            var count = await ConnectionManager.Count().ConfigureAwait(false);
            if (count == 0)
                Client.ConnectToSeedNodes();
        }
    }
}