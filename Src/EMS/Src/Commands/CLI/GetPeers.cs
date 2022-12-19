using AngryWasp.Cli;
using AngryWasp.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("peers", "Print a list of connected peers")]
    public class GetPeers : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {

            List<Connection> disconnected = new List<Connection>();
            
#region Incoming

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Incoming:");

            Console.ForegroundColor = ConsoleColor.Green;

            int count = 0;
            await ConnectionManager.ForEach(Direction.Incoming, (c) =>
            {
                try {
                    Console.WriteLine($"{c.PeerId} - {c.Address.MapToIPv4()}:{c.Port}");
                    ++count;
                } catch {
                    disconnected.Add(c);
                }
            }).ConfigureAwait(false);

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

#endregion

            Console.WriteLine();

#region Outgoing

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Outgoing:");

            Console.ForegroundColor = ConsoleColor.Green;

            count = 0;
            
            await ConnectionManager.ForEach(Direction.Outgoing, (c) =>
            {
                try {
                    Console.WriteLine($"{c.PeerId} - {c.Address.MapToIPv4()}:{c.Port}");
                    ++count;
                } catch {
                    disconnected.Add(c);
                }
                
            }).ConfigureAwait(false);


            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

#endregion

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            foreach (var d in disconnected)
                await ConnectionManager.RemoveAsync(d, null).ConfigureAwait(false);

            return true;
        }
    }
}