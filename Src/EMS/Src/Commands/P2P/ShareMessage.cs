using AngryWasp.Cli;
using AngryWasp.Net;
using AngryWasp.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AngryWasp.Logger;

namespace EMS.Commands.P2P
{
    public static class ShareMessage
    {
        public const byte CODE = 11;

        static SemaphoreSlim lockObject = new SemaphoreSlim(1, 1);

        public static List<byte> GenerateRequest(bool isRequest, byte[] message)
        {
            return Header.Create(CODE, isRequest, (ushort)(message.Length))
                .Join(message);
        }

        public static async Task GenerateResponse(Connection c, Header h, byte[] d)
        {
            lockObject.Wait();

            try
            {
                Message msg;

                if (!d.Validate(true, out msg))
                {
                    await c.AddFailureAsync("ShareMessage: Message failed validation").ConfigureAwait(false);
                    return;
                }

                if (MessagePool.Messages.TryAdd(msg.Key, msg))
                {
                    KeyRing.DecryptMessage(ref msg);
                    if (msg.IsDecrypted)
                    {
                        if (MessagePool.AddMessageToCache(msg.Key, msg.Data))
                        {
                            if (Config.User.AutoreadMessages)
                                Helpers.WriteMessageToConsole(msg);
                            else
                                Log.Instance.Write($"Received a message with key {msg.Key}");
                        }
                    }
                }
                else
                    //already have it. skip. cause we already would have shared this the first time we got it
                    return;

                byte[] request = ShareMessage.GenerateRequest(true, d).ToArray();

                List<Connection> disconnected = new List<Connection>();

                await ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, async (con) =>
                {
                    if (con.PeerId == h.PeerID)
                        return;

                    await con.WriteAsync(request).ConfigureAwait(false);
                }).ConfigureAwait(false);

                foreach (var disc in disconnected)
                    await ConnectionManager.RemoveAsync(disc, "Not connected").ConfigureAwait(false);
            }
            finally { lockObject.Release(); }
        }
    }
}