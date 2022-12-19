using System.Collections.Generic;
using System.IO;
using AngryWasp.Logger;

namespace EMS
{
    public static class AddressBook
    {
        private static Dictionary<string, AddressBookEntry> entries = new Dictionary<string, AddressBookEntry>();
        
        private static Dictionary<string, string> reverseLookup = new Dictionary<string, string>();

        public static Dictionary<string, AddressBookEntry> Entries => entries;

        public static Dictionary<string, string> ReverseLookup => reverseLookup;

        public static bool IsNameOK(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (entries.ContainsKey(name))
                return false;

            return true;
        }

        public static void Load(string password)
        {
            string path = Path.GetDirectoryName(Config.User.Paths.KeyFile);
            var hashedPassword = KeyEncryption.HashPassword(password);

            var contactFiles = Directory.GetFiles(path, "*.contact");

            foreach (var c in contactFiles)
            {
                var e = AddressBookEntry.CreateFromFile(c, hashedPassword);
                if (c != null)
                {
                    if (e != null)
                    {
                        entries.Add(e.ID, e);
                        foreach (var k in e.Keys)
                            reverseLookup.Add(k.Address, e.ID);
                    }
                    else
                        Log.Instance.WriteWarning($"Could not load contact file '{c}'");
                }
            }
        }
    }
}