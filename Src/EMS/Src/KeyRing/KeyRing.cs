using System.Collections;
using System.IO;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using System.Linq;
using AngryWasp.Cli;
using System.Collections.Generic;
using AngryWasp.Logger;
using AngryWasp.Random;
using AngryWasp.Cli.Prompts;
using AngryWasp.Math;

namespace EMS
{
    public static class KeyRing
    {
        private static readonly XoShiRo128PlusPlus rng = new XoShiRo128PlusPlus();
        private static List<Key> keys = new List<Key>();

        public static List<Key> Keys => keys;

        public static void PrintKeys()
        {
            for (int i = 0; i < keys.Count; i++)
                Log.Instance.Write($"Key {i.ToString().PadLeft(2)}: {keys[i].Base58Address}");
        }

        public static void LoadKeyRing(string password = null)
        {
            if (!File.Exists(Config.User.Paths.KeyFile))
            {
                CreateKeyRing();
                return;
            }

            if (string.IsNullOrEmpty(password))
                PasswordPrompt.Get(out password, "Enter your key file password");

            try
            {
                var encryptedKeyData = File.ReadAllBytes(Config.User.Paths.KeyFile);
                keys = KeyEncryption.Decrypt(encryptedKeyData, password);
                PrintKeys();
            }
            catch
            {
                Application.TriggerExit("Error loading key ring. Aborting!");
            }

            AddressBook.Load(password);
        }

        public static void CreateKeyRing(string password = null)
        {
            keys = CreateKeyRing(Config.User.NumKeys);
            
            if (password == null)
                password = Helpers.DisplayPasswordPrompt();

            try
            {
                var encryptedKeyData = KeyEncryption.Encrypt(keys, password);
                File.WriteAllBytes(Config.User.Paths.KeyFile, encryptedKeyData);
                PrintKeys();
            }
            catch
            {
                Application.TriggerExit("Error creating key ring. Aborting!");
            }
        }

        private static List<Key> CreateKeyRing(int count)
        {
            List<Key> tempRing = new List<Key>();
            for (int i = 0; i < count; i++)
            {
                var (pubKey, priKey) = Ecc.GenerateKeyPair();
                tempRing.Add(Key.Create(pubKey, priKey));
            }

            return tempRing;
        }

        public static void EraseKeys() => keys.Clear();

        public static bool EncryptMessage(byte[] input, string base58RecipientAddress, out byte[] encryptionResult, out byte[] signature, out byte[] addressXor, out int keyIndex)
        {
            byte[] to;
            encryptionResult = null;
            signature = null;
            addressXor = null;
            keyIndex = 0;
            if (!Base58.Decode(base58RecipientAddress, out to))
            {
                Log.Instance.WriteWarning("Address is invalid");
                return false;
            }

            keyIndex = rng.Next(0, keys.Count - 1);

            Key randomkey = keys[keyIndex];

            byte[] sharedKey = KeyEncryption.CreateSharedKey(to, randomkey);

            if (sharedKey == null)
            {
                Log.Instance.WriteWarning("Address is invalid");
                return false;
            }

            BitArray a = new BitArray(randomkey.PublicKey);
            BitArray b = new BitArray(to);
            addressXor = new byte[randomkey.PublicKey.Length];

            a.Xor(b);
            a.CopyTo(addressXor, 0);

            encryptionResult = Aes.Encrypt(input, sharedKey);

            //Mitigation for known key attack vector.
            //problem. a malicious actor knows someones address. They can use that to determine the message sender and derive the counter-parties address
            //Example. Alice sends Bob a message. The Address-XOR is a combination of both Alice and Bobs addresses.
            //A malicious actor (Kevin) knows Alice's address cause she was stupid and posted it on reddit.
            //kevin can test if the message came from Alice by verifying the message signature against Alice's address which he knows
            //if the result is OK, then kevin now knows the message came from Alice.
            //Additionally, Kevin can then XOR Alice's address into the message and derive Bob's address. Now Kevin knows alice and Bob's addresses
            //Kevin can then exmand his tracability efforts and via the same process expose addresses and trace any messages Bob sends with that address and so on
            
            //Solution: Encrypt the message signature. The sender must then decrypt it with the same shared key they use to decrypt the message
            //Then kevin would have to know the associated private key to decrypt the signature before using it to verify the message

            var plainTextSignature = Ecc.Sign(encryptionResult, randomkey.PrivateKey);
            signature = Aes.Encrypt(plainTextSignature, sharedKey);

            return true;
        }

        public static void DecryptMessage(ref Message inputMessage)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (DecryptMessage(ref inputMessage, i))
                {
                    //Log.Instance.Write($"Decrypted message with Key {i}: {keys[i].Base58Address}");
                    return;
                }
            }
        }
        
        // The before being sent for decryption a message should have already been validated
        // The validation process extracts some message properties, so we pass in the 
        // validated message to fill the remaining data
        private static bool DecryptMessage(ref Message inputMessage, int decryptionKeyIndex)
        {
            byte[] inputData = inputMessage.Data.Skip(45).ToArray();
            Key decryptionKey = keys[decryptionKeyIndex];

            //make sure we have enough data to read the fixed length input we expect
            int expectedLength = 101;
            if (inputData.Length < expectedLength)
            {
                Log.Instance.WriteError("Message decryption error. Message is corrupt");
                return false;
            }

            BinaryReader reader = new BinaryReader(new MemoryStream(inputData));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            ushort signatureLength = reader.ReadBytes(2).ToUShort();
            ushort encryptedMessageLength = reader.ReadBytes(2).ToUShort();
            BitArray xorKey = new BitArray(reader.ReadBytes(65));
            HashKey32 readProofHash = reader.ReadBytes(32);
            byte[] xorResult = new byte[65];

            BitArray ba = new BitArray(decryptionKey.PublicKey);
            ba.Xor(xorKey);
            ba.CopyTo(xorResult, 0);

            expectedLength += (signatureLength + encryptedMessageLength);
            if (inputData.Length < expectedLength)
            {
                Log.Instance.WriteError("Message decryption error. Message is corrupt");
                return false;
            }
    
            byte[] encryptedSignature = reader.ReadBytes(signatureLength);
            byte[] encryptedMessage = reader.ReadBytes(encryptedMessageLength);

            reader.Close();

            // If we can verify this against the xor result, it means it was validated 
            // against a senders address making it an imcoming message
            //
            // If that fails, we try to validate it against our own public key. This would indicate it is an 
            // outgoing message. We may need this in case we sync messages we previously sent from the pool
            // i.e. if we restarted a node. Without this code the verification would fail and they would show as encrypted
            var decryptionSharedKey = KeyEncryption.CreateSharedKey(xorResult, decryptionKey);

            if (!Aes.Decrypt(encryptedSignature, decryptionSharedKey, out byte[] signature))
                return false;
            
            if (Ecc.Verify(encryptedMessage, xorResult, signature))
                inputMessage.Direction = Message_Direction.In;
            else if (Ecc.Verify(encryptedMessage, decryptionKey.PublicKey, signature))
                inputMessage.Direction = Message_Direction.Out;
            else
                return false;

            if (!Aes.Decrypt(encryptedMessage, decryptionSharedKey, out byte[] decrypted))
            {
                Log.Instance.WriteError("Message decryption error. Message is corrupt");
                return false;
            }
            
            if (decrypted == null)
            {
                Log.Instance.WriteError("Message passed verification, but failed decryption. Suspect message tampering.");
                return false;
            }

            HashKey16 readProofNonce = decrypted.Take(16).ToArray();
            inputMessage.MessageType = (Message_Type)decrypted[16];
            inputMessage.DecryptionData = new DecryptionData(decrypted.Skip(17).ToArray(), decryptionKeyIndex);
            inputMessage.Address = Base58.Encode(xorResult);
            inputMessage.ReadProof = new ReadProof
            {
                Nonce = readProofNonce,
                Hash = readProofHash
            };

            return true;
        }
    
        public static List<byte> ToByte()
        {
            List<byte> data = new List<byte>();
            foreach (var k in keys)
                data.AddRange(k.PublicKey);

            return data;
        }
    }
}