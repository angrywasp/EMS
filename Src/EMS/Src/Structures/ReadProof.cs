using AngryWasp.Cryptography;
using Newtonsoft.Json;
using CryptoHelper = AngryWasp.Cryptography.Helper;

namespace EMS
{
    public class ReadProof
    {
        [JsonProperty("nonce")]
        public HashKey16 Nonce { get; set; } = HashKey16.Empty;

        [JsonProperty("hash")]
        public HashKey32 Hash { get; set; } = HashKey32.Empty;

        [JsonProperty("read")]
        public bool IsRead { get; set; } = false;

        public static ReadProof Create()
        {
            HashKey16 nonce = CryptoHelper.GenerateSecureBytes(16);
            HashKey32 hash = GenerateHash(nonce);

            return new ReadProof
            {
                Nonce = nonce,
                Hash = hash
            };
        }

        public static HashKey32 GenerateHash(HashKey16 key) => Keccak.Hash256(key.ToByte());
    }
}