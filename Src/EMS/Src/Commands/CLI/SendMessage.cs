using AngryWasp.Cli;
using AngryWasp.Logger;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("send", "Send a message. Usage: send <contact-name/address> <message>")]
    public class SendMessage : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.Instance.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            
            string address = Helpers.PopWord(ref command);
            AddressBookEntry entry;
            if (AddressBook.Entries.TryGetValue(address, out entry))
                address = entry.RandomAddress();

            var sendResult = await MessagePool.Send(address, Message_Type.Text, Encoding.ASCII.GetBytes(command), Config.User.MessageExpiration).ConfigureAwait(false);

            if (sendResult.SentOk)
                Log.Instance.Write($"Sent message with key {sendResult.MessageKey}");
            else
                Log.Instance.WriteError($"Failed to send message");

            return sendResult.SentOk;
        }
    }
}