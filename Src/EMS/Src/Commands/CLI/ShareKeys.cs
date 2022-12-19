using AngryWasp.Cli;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("share_keys", "Send your public keys to a contact: share_keys <your-name> <address>")]
    public class ShareKeys : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.Instance.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            
            var address = Helpers.PopWord(ref command);
            AddressBookEntry entry;
            if (AddressBook.Entries.TryGetValue(address, out entry))
                address = entry.RandomAddress();

            var message = KeyRing.ToByte().Join(Encoding.ASCII.GetBytes(Config.User.ClientName)).ToArray();
            var sendResult = await MessagePool.Send(address, Message_Type.AddressList, message, Config.User.MessageExpiration).ConfigureAwait(false);

            if (sendResult.SentOk)
                Log.Instance.Write($"Sent message with key {sendResult.MessageKey}");
            else
                Log.Instance.WriteError($"Failed to send message");

            return sendResult.SentOk;
        }
    }
}