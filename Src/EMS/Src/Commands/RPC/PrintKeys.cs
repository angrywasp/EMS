using Newtonsoft.Json;
using AngryWasp.Helpers;
using AngryWasp.Json.Rpc;
using System.Collections.Generic;
using System.Threading.Tasks;
using EMS.Commands.RPC;
using System;

namespace EMS.Commands.Rpc
{
    [JsonRpcServerCommand("print_keys")]
    public class printKeys : IJsonRpcServerCommand
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

                foreach (var key in KeyRing.Keys)
                {
                    JsonKeyData k = new JsonKeyData();
                    k.Address = key.Base58Address;
                    k.PublicKey = key.PublicKey.ToHex();

                    if (request.Data.Private)
                        k.PrivateKey = key.PrivateKey.ToHex();

                    response.Data.Keys.Add(k);
                }

                return new JsonRpcServerCommandResult { Success = true, Value = response };
            }
            catch (Exception ex)
            {
                return Error.Generate($"Exception: {ex.Message}", 666);
            }
        }

        public class Request
        {
            [JsonProperty("private")]
            public bool Private { get; set; } = false;
        }

        public class Response
        {
            public List<JsonKeyData> Keys { get; set; } = new List<JsonKeyData>();
        }

        public class JsonKeyData
        {
            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("public")]
            public string PublicKey { get; set; } = string.Empty;

            [JsonProperty("private")]
            public string PrivateKey { get; set; } = string.Empty;
        }
    }
}