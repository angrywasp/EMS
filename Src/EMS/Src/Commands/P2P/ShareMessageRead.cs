using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Commands.P2P
{
    public static class ShareMessageRead
    {
        public const byte CODE = 13;

        public static List<byte> GenerateRequest(bool isRequest, byte[] message)
        {
            return Header.Create(CODE, isRequest, (ushort)(message.Length))
                .Join(message);
        }

        public static async Task GenerateResponse(Connection c, Header h, byte[] d)
        {
            HashKey16 messageKey = d.Take(16).ToArray();
            HashKey16 readProofNonce = d.Skip(16).Take(16).ToArray();

            Message msg = null;
            if (!MessagePool.Messages.TryGetValue(messageKey, out msg))
            {
                //Don't register a failure against the peer. we may have just received the read proof and not the message
                //requesting the message pool will gather these later
                //Log.Instance.WriteWarning($"{CommandCode.CommandString(CODE)}: Verification failed. Orphan");
                //c.AddFailure();
                return;
            }

            HashKey32 readProofHash = ReadProof.GenerateHash(readProofNonce);
            
            if (readProofHash != msg.ExtractReadProofHash())
            {
                await c.AddFailureAsync("ShareMessageRead: Message failed validation").ConfigureAwait(false);
                return;
            }

            msg.ReadProof = new ReadProof
            {
                Nonce = readProofNonce,
                Hash = readProofHash,
                IsRead = true
            };

            byte[] req = ShareMessageRead.GenerateRequest(true, d).ToArray();

            await ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, async (con) =>
            {
                if (con.PeerId == h.PeerID)
                    return; //don't return to sender

                await con.WriteAsync(req).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}