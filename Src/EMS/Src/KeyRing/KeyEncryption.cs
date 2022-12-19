using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AngryWasp.Cryptography;

namespace EMS
{
    public class KeyEncryptionException : Exception
    {
        public KeyEncryptionException(string message) : base(message) { }
    }

    public static class KeyEncryption
    {
        public static byte[] HashPassword(string password = null) =>
            string.IsNullOrEmpty(password) ? Keccak.Hash128(HashKey16.Empty) : Keccak.Hash128(Encoding.ASCII.GetBytes(password));

        public static byte[] Encrypt(List<Key> keyRing, string password = null)
        {
            if (keyRing.Count == 0)
                throw new KeyEncryptionException("key ring is empty!");

            var decryptedkeyData = new byte[keyRing.Count * 32];

            for (int i = 0; i < keyRing.Count; i++)
                Buffer.BlockCopy(keyRing[i].PrivateKey, 0, decryptedkeyData, i * 32, 32);

            return Aes.Encrypt(decryptedkeyData, HashPassword(password));
        }

        public static List<Key> Decrypt(byte[] encryptedKeyData, string password = null)
        {
            byte[] decryptedKeyData = null;
            List<Key> tempKeys = new List<Key>();

            try
            {
                Aes.Decrypt(encryptedKeyData, HashPassword(password), out decryptedKeyData);
            }
            catch
            {
                throw new KeyEncryptionException("The password was incorrect");
            }

            if (decryptedKeyData.Length % 32 != 0)
                throw new KeyEncryptionException("Decryped key ring data is incorrect length");

            using (MemoryStream ms = new MemoryStream(decryptedKeyData))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    int count = decryptedKeyData.Length / 32;

                    for (int i = 0; i < count; i++)
                    {
                        byte[] priKey = reader.ReadBytes(32);
                        byte[] pubKey = Ecc.GetPublicKeyFromPrivateKey(priKey);
                        tempKeys.Add(Key.Create(pubKey, priKey));
                    }
                }
            }

            return tempKeys;
        }

        public static byte[] CreateSharedKey(byte[] recipientPublicKey, Key encryptionKey) => 
            Ecc.CreateKeyAgreement(encryptionKey.PrivateKey, recipientPublicKey);
    }
}