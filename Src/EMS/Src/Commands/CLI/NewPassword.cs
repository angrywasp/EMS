using AngryWasp.Cli;
using AngryWasp.Cli.Prompts;
using AngryWasp.Logger;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("new_password", "Change the password for the loaded key file")]
    public class NewPassword : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.Instance.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }

            //We confirm the old password by attempting to decrypt the key file
            var encryptedKeyData = File.ReadAllBytes(Config.User.Paths.KeyFile);
            PasswordPrompt.Get(out string oldPassword, "Enter your current password");

            try
            {
                KeyEncryption.Decrypt(encryptedKeyData, oldPassword);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Key file password was incorrect.");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            string newPassword = Helpers.DisplayPasswordPrompt();

            encryptedKeyData = KeyEncryption.Encrypt(KeyRing.Keys, newPassword);
            File.WriteAllBytes(Config.User.Paths.KeyFile, encryptedKeyData);
            return true;
        }
    }
}