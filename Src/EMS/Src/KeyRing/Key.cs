using AngryWasp.Cli;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using Newtonsoft.Json;

namespace EMS
{
    public class Key
    {
        private byte[] privateKey;
        private byte[] publicKey;
        private string base58Address;

        public byte[] PrivateKey => privateKey;
        public byte[] PublicKey => publicKey;
        public string Base58Address => base58Address;

        public static Key Create(byte[] pubKey, byte[] priKey)
        {
            var b58 = Base58.Encode(pubKey);

            return new Key
            {
                privateKey = priKey,
                publicKey = pubKey,
                base58Address = b58
            };
        }

        public override string ToString() => base58Address;
    }
}