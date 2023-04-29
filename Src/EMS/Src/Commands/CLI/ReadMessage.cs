using System;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using AngryWasp.Logger;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("read", "Read a message. Usage: read <message_hash>")]
    public class ReadMessage : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.Instance.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            

            string hex = Helpers.PopWord(ref command);

            if (string.IsNullOrEmpty(hex))
            {
                Log.Instance.WriteError("Incorrect number of arguments");
                return false;
            }

            if (hex.Length != 32)
            {
                Log.Instance.WriteError("Invalid argument");
                return false;
            }

            HashKey16 key = hex.FromByteHex();

            Message message;

            if (!MessagePool.Messages.TryGetValue(key, out message))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Message not found.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            if (!message.IsDecrypted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Message is encrypted. Cannot read.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            Helpers.WriteMessageToConsole(message);
            return true;
        }
    }
}