using AngryWasp.Json.Rpc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Commands.RPC
{
    [JsonRpcServerCommand("get_message_count")]
    public class GetMessageCount : IJsonRpcServerCommand
    {
        public async Task<JsonRpcServerCommandResult> Handle(string requestString)
        {
            try
            {
                int total = MessagePool.Messages.Count;
                int decrypted = MessagePool.Messages.Where(x => x.Value.IsDecrypted).Count();

                JsonResponse<Response> response = new JsonResponse<Response>();
                response.Data.Total = total;
                response.Data.Decrypted = decrypted;

                return new JsonRpcServerCommandResult { Success = true, Value = response };
            }
            catch (Exception ex)
            {
                return Error.Generate($"Exception: {ex.Message}", 666);
            }
        }

        public class Response
        {
            [JsonProperty("total")]
            public int Total { get; set; } = 0;

            [JsonProperty("decrypted")]
            public int Decrypted { get; set; } = 0;
        }
    }
}