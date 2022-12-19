using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Helpers;
using AngryWasp.Logger;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("mark_read", "Mark a message as read. Usage: flag <message_hash>")]
    public class MarkMessageRead : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            string hex = Helpers.PopWord(ref command);

            if (string.IsNullOrEmpty(hex) || hex.Length != 32)
            {
                Log.Instance.WriteError("Incorrect number of arguments");
                return false;
            }

            if (hex.Length != 32)
            {
                Log.Instance.WriteError("Invalid argument");
                return false;
            }

            var marked = await MessagePool.MarkMessageRead(hex.FromByteHex()).ConfigureAwait(false);
            return marked;
        }
    }
}
