using System.Threading.Tasks;
using AngryWasp.Cli;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("read_all", "Read all unread messages")]
    public class ReadAllMessages : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            foreach (var m in MessagePool.Messages.Values)
            {
                if (m.IsDecrypted && 
                    m.Direction == Message_Direction.In &&
                    !m.ReadProof.IsRead)
                    Helpers.WriteMessageToConsole(m);
            }

            return true;
        }
    }
}