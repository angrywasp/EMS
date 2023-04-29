using AngryWasp.Cryptography;
using AngryWasp.Json.Rpc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace EMS.Commands.RPC
{
    [JsonRpcServerCommand("get_message")]
    public class GetMessage : IJsonRpcServerCommand
    {
        public async Task<JsonRpcServerCommandResult> Handle(string requestString)
        {
            try
            {
                if (string.IsNullOrEmpty(requestString))
                    return Error.Generate("Empty request string", 10);

                if (!JsonRequest<Request>.Deserialize(requestString, out JsonRequest<Request> request))
                    return Error.Generate("Invalid JSON", 11);

                JsonResponse<Response> response = new JsonResponse<Response>();

                if (!MessagePool.Messages.TryGetValue(request.Data.Key, out Message message))
                    return Error.Generate("Could not find message in pool", 20);

                response.Data.Key = message.Key;
                response.Data.Hash = message.Hash;
                response.Data.Timestamp = message.Timestamp;
                response.Data.Expiration = message.Expiration;
                response.Data.MessageVersion = message.MessageVersion;
                response.Data.MessageType = message.MessageType;

                response.Data.Direction = message.Direction.ToString().ToLower();

                if (message.ReadProof != null)
                    response.Data.ReadProof = message.ReadProof;

                if (!message.IsDecrypted)
                    return Error.Generate("Message is not decrypted", 30);

                response.Data.Address = message.Address;
                response.Data.Message = new JsonDecryptionData
                {
                    Base64Message = Convert.ToBase64String(message.DecryptionData.Data),
                    KeyIndex = message.DecryptionData.KeyIndex,
                    Address = message.DecryptionData.Address
                };

                return new JsonRpcServerCommandResult { Success = true, Value = response };
            }
            catch (Exception ex)
            {
                return Error.Generate($"Exception: {ex.Message}", 666);
            }
        }

        public class Request
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;
        }

        public class JsonDecryptionData
        {
            [JsonProperty("content")]
            public string Base64Message { get; set; } = string.Empty;

            [JsonProperty("key_index")]
            public int KeyIndex { get; set; } = -1;

            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;
        }

        public class Response
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;

            [JsonProperty("hash")]
            public HashKey32 Hash { get; set; } = HashKey32.Empty;

            [JsonProperty("timestamp")]
            public uint Timestamp { get; set; } = 0;

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 0;

            [JsonProperty("version")]
            public byte MessageVersion { get; set; } = 0;

            [JsonProperty("type")]
            public Message_Type MessageType { get; set; } = Message_Type.Invalid;

            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("message")]
            public JsonDecryptionData Message { get; set; } = new JsonDecryptionData();

            [JsonProperty("direction")]
            public string Direction { get; set; } = string.Empty;

            [JsonProperty("read_proof")]
            public ReadProof ReadProof { get; set; } = new ReadProof();
        }
    }
}