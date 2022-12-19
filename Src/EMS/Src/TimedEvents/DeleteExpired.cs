using System.Collections.Generic;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Logger;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60)]
    public class DeleteExpired : ITimedEvent
    {
        public async Task Execute()
        {
            HashSet<HashKey16> delete = new HashSet<HashKey16>();

            foreach (var m in MessagePool.Messages)
            {
                if (m.Value.IsExpired())
                    delete.Add(m.Key);
            }

            foreach (var k in delete)
            {
                Message m;
                MessagePool.Messages.TryRemove(k, out m);
                Log.Instance.Write($"Message {m.Key} expired");
            }
        }
    }
}