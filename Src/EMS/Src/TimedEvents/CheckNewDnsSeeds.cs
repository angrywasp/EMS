using System.Threading.Tasks;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60 * 60)]
    public class CheckNewDnsSeeds : ITimedEvent
    {
        public async Task Execute()
        {
            if (!Config.User.NoDnsSeeds)
                Helpers.AddSeedFromDns(Config.User.NetID, Config.User.SeedDns);
        }
    }
}