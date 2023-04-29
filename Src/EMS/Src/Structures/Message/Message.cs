using System;
using System.IO;
using System.Linq;
using System.Text;
using AngryWasp.Cli.Prompts;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Random;

namespace EMS
{
    public class Message
    {
        public HashKey16 Key { get; set; } = HashKey16.Empty;
        public HashKey32 Hash { get; set; } = HashKey32.Empty;
        public byte[] Data { get; set; } = null;
        public byte MessageVersion { get; set; } = 0;
        public Message_Type MessageType { get; set; } = Message_Type.Invalid;
        public uint Timestamp { get; set; } = 0;
        public uint Expiration { get; set; } = 0;
        public string Address { get; set; } = string.Empty;
        public DecryptionData DecryptionData { get; set; } = null;
        public Message_Direction Direction { get; set; } = Message_Direction.None;
        public ReadProof ReadProof { get; set; } = new ReadProof();

        public bool IsDecrypted => !string.IsNullOrEmpty(Address) && DecryptionData != null;

        public HashKey32 ExtractReadProofHash() => Data.Skip(114).Take(32).ToArray();

        public bool IsExpired()
        {
            ulong expireTime = Timestamp + Expiration;

            if (DateTimeHelper.TimestampNow > expireTime)
                return true;

            return false;
        }

        //returns true if less than 5 minutes to expiration
        public bool IsNearlyExpired()
        {
            ulong expireTime = Timestamp + (Expiration - 300);

            if (DateTimeHelper.TimestampNow > expireTime)
                return true;

            return false;
        }

        public void ParseDecryptedMessage()
        {
            if (MessageType >= Message_Type.Invalid)
                Console.WriteLine($"MessageType {((byte)MessageType).ToString()} is invalid");

            switch (MessageType)
            {
                case Message_Type.Text:
                    Console.WriteLine(Encoding.ASCII.GetString(DecryptionData.Data));
                    break;
                case Message_Type.AddressList:
                    {
                        if (Direction == Message_Direction.Out)
                            return; //we may have synced a message we previously sent out. ignore it
                            
                        //Extract the name from the key list. May as well parse the entire thing
                        AddressBookEntry l = AddressBookEntry.CreateFromMessage(DecryptionData.Data);

                        bool exists = AddressBook.Entries.ContainsKey(l.ID);
                        bool newKeys = false;
                        bool update = false;
                        if (exists)
                            newKeys = AddressBook.Entries[l.ID].Key != l.Key;

                        if (exists)
                        {
                            if (!newKeys)
                                return;
                            else
                                update = true;
                        }
                            
                        var r = CliPrompt.Question($"You have received {(update ? "an updated": "a new")} key list from {l.ID}. Do you want to save it?");

                        if (r == CLIPrompt_Response.Yes)
                        {
                            string name = l.ID;

                            while (true)
                            {
                                if (update)
                                {
                                    //delete file and remove from key ring
                                    File.Delete(l.FilePath);
                                    AddressBook.Entries.Remove(l.ID);
                                }
                                
                                name = CliPrompt.UserInput($"Enter a name for this contact or leave empty to accept '{l.ID}'");
                                if (string.IsNullOrEmpty(name))
                                    name = l.ID;

                                if (!AddressBook.IsNameOK(name))
                                {
                                    Console.WriteLine("Name is empty or already in use. Please try again");
                                    continue;
                                }

                                break;
                            }
                            
                            l.ChangeName(name);

                            var encryptedKeyData = File.ReadAllBytes(Config.User.Paths.KeyFile);
                            string password;

                            while (true)
                            {
                                PasswordPrompt.Get(out password, "Enter your key file password");
                            
                                try
                                {
                                    KeyEncryption.Decrypt(encryptedKeyData, password);
                                    break;
                                }
                                catch
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Key file password was incorrect.");
                                    Console.ForegroundColor = ConsoleColor.White;
                                }
                            }

                            byte[] newEntryData = l.ToByte();
                            var hashedPassword = KeyEncryption.HashPassword(password);
                            var encryptedEntryData = AngryWasp.Cryptography.Aes.Encrypt(newEntryData, hashedPassword);
                            string path = null;
                            while (true)
                            {
                                path = Path.Combine(Config.User.DataDir, $"{RandomString.AlphaNumeric(16)}.contact");

                                if (!File.Exists(path))
                                    break;
                            }

                            File.WriteAllBytes(path, encryptedEntryData);
                            AddressBook.Entries.Add(l.ID, l);
                            foreach (var k in l.Keys)
                            {
                                if (AddressBook.Entries.ContainsKey(k.Address))
                                {
                                    string conflictingID = AddressBook.ReverseLookup[k.Address];
                                    Log.Instance.WriteWarning($"Attempting to save address {k.Address} for user {l.ID}, but it already exists for user {conflictingID}");
                                    continue;
                                }

                                AddressBook.ReverseLookup.Add(k.Address, l.ID);
                            }

                            Log.Instance.Write($"Contact information for {l.ID} saved to {path}");
                        }
                    }
                    break;
                default:
                    Console.WriteLine($"MessageType {((byte)MessageType).ToString()} is not supported");
                    break;
            }
        }
    }
}