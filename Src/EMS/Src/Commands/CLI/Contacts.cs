using System;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Net;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("contacts", "Show a list of your contacts")]
    public class Contacts : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            foreach (var e in AddressBook.Entries)
                Console.WriteLine(e.Value.ID, e.Value.FilePath);
            return true;
        }
    }
}