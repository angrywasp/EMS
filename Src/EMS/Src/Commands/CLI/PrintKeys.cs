using AngryWasp.Cli;
using AngryWasp.Logger;
using System.Threading.Tasks;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("print_keys", "Prints your messaging address")]
    public class PrintKeys : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.Instance.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            
            KeyRing.PrintKeys();
            return true;
        }
    }
}