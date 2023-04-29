using System.Collections.Generic;
using System.IO;
using System.Text;
using AngryWasp.Cryptography;
using AngryWasp.Random;

namespace EMS
{
    public class AddressBookEntry
    {
        private static readonly XoShiRo128PlusPlus rng = new XoShiRo128PlusPlus();
        private const int KEY_LENGTH = 65;
        private List<AddressBookKey> keys = new List<AddressBookKey>();
        private string id;
        private HashKey32 key;
        private string filePath;

        public string ID => id;
        public HashKey32 Key => key;
        public string FilePath => filePath;

        public List<AddressBookKey> Keys => keys;

        public void ChangeName(string name) => id = name;

        public static AddressBookEntry CreateFromFile(string path, byte[] password)
        {
            var encryptedData = File.ReadAllBytes(path);

            try
            {
                Aes.Decrypt(encryptedData, password, out byte[] decryptedData);
                var e = CreateFromMessage(decryptedData);
                e.filePath = path;
                return e;
            }
            catch
            {
                return null;
            }
        }

        public static AddressBookEntry CreateFromMessage(byte[] message)
        {
            int keyCount = message.Length / KEY_LENGTH;
            int nameLength = message.Length - (keyCount * KEY_LENGTH);

            AddressBookEntry e = new AddressBookEntry();
            e.key = HashKey32.Make(message);

            using (MemoryStream ms = new MemoryStream(message))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    for (int i = 0; i < keyCount; i++)
                        e.keys.Add(AddressBookKey.Create(reader.ReadBytes(KEY_LENGTH)));

                    e.id = Encoding.ASCII.GetString(reader.ReadBytes(nameLength));
                }
            }

            return e;
        }

        public byte[] ToByte()
        {
            var r = new List<byte>();
            foreach (var k in keys)
                r.AddRange(k.Key);

            r.AddRange(Encoding.ASCII.GetBytes(id));

            return r.ToArray();
        }

        public string RandomAddress()
        {
            int r = rng.Next(0, keys.Count - 1);
            return keys[r].Address;
        }
    }
}