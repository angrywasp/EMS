using System;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("read_cached", "Read all cached messages")]
    public class ReadCachedMessages : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            foreach (var cm in MessagePool.MessageCache.Values)
            {
                Message m;
                if (cm.Validate(false, out m))
                    if (m.IsDecrypted)
                        Helpers.WriteMessageToConsole(m);
            }

            return true;
        }
    }
}