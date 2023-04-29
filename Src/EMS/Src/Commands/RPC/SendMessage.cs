using AngryWasp.Cryptography;
using AngryWasp.Json.Rpc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace EMS.Commands.RPC
{
    [JsonRpcServerCommand("send_message")]
    public class SendMessage : IJsonRpcServerCommand
    {
        public async Task<JsonRpcServerCommandResult> Handle(string requestString)
        {
            try
            {
                if (string.IsNullOrEmpty(requestString))
                    return Error.Generate("Empty request string", 10);

                if (!JsonRequest<Request>.Deserialize(requestString, out JsonRequest<Request> request))
                    return Error.Generate("Invalid JSON", 11);

                if (Config.User.RelayOnly)
                    return Error.Generate("Node is a relay node. Node has no keys", 100);

                JsonResponse<Response> response = new JsonResponse<Response>();

                byte[] messageBytes = Convert.FromBase64String(request.Data.Message);

                var sendResult = await MessagePool.Send(request.Data.Address, Message_Type.Text, messageBytes, request.Data.Expiration).ConfigureAwait(false);

                if (!sendResult.SentOk)
                    return Error.Generate("Failed to send message", 200);

                response.Data.Key = sendResult.MessageKey;

                return new JsonRpcServerCommandResult { Success = true, Value = response };
            }
            catch (Exception ex)
            {
                return Error.Generate($"Exception: {ex.Message}", 666);
            }
        }

        public class Request
        {
            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 3600;
        }

        public class Response
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;
        }
    }
}