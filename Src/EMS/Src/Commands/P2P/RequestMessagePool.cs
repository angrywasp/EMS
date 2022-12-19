using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Commands.P2P
{
    public static class RequestMessagePool
    {
        public const byte CODE = 12;

        static SemaphoreSlim lockObject = new SemaphoreSlim(1, 1);

        public static List<byte> GenerateRequest(bool isRequest)
        {
            // We construct a message that lists all of the message hashes we have
            // along with a byte flag that indicates if this message is read

            List<byte> message = new List<byte>();
            foreach (var m in MessagePool.Messages)
            {
                message.AddRange(m.Key);
                message.Add(m.Value.ReadProof.IsRead ? (byte)1 : (byte)0);
            }

            return Header.Create(CODE, isRequest, (ushort)(MessagePool.Messages.Count * 17))
                .Join(message);
        }

        // This function only ever returns null. it does this because there is potential for the response to be send in multiple messages
        // and it is not being used internally anyway, so easiest path it to just not return any data
        public static async Task GenerateResponse(Connection c, Header h, byte[] d)
        {
            if (h.IsRequest)
            {
                //reconstruct the message data to a list of keys to compare with
                Dictionary<HashKey16, byte> hashes = new Dictionary<HashKey16, byte>();

                if (d.Length > 0)
                {
                    BinaryReader reader = new BinaryReader(new MemoryStream(d));

                    int count = d.Length / 17;
                    for (int i = 0; i < count; i++)
                        hashes.Add(reader.ReadBytes(16), reader.ReadByte());

                    reader.Close();
                }

                // The max message length of the p2p protocol is ushort.MaxValue
                // So we iterate all the messages in our local pool and send them
                // Splitting the response into multiple messages if required

                List<byte> payload = new List<byte>();

                foreach (var m in MessagePool.Messages)
                {
                    bool sendPruned = true;

                    //don't include a message that has expired byt is till in our pool
                    //it will be cleaned up eventually
                    if (m.Value.IsExpired())
                        continue;

                    if (hashes.ContainsKey(m.Key))
                    {
                        byte comp = m.Value.ReadProof.IsRead ? (byte)1 : (byte)0;

                        // Skip. Read status matches or the incoming data says this message is read
                        if (hashes[m.Key] == comp || hashes[m.Key] == 1)
                            continue;
                    }
                    else
                        sendPruned = false; // Requesting node does not have this message. Send full

                    HashKey16 readProofNonce = HashKey16.Empty;
                    if (m.Value.ReadProof.IsRead)
                        readProofNonce = m.Value.ReadProof.Nonce;

                    List<byte> entry = m.Key.ToList().Join(readProofNonce);

                    // If send pruned is true, it means that the requesting node already has this message
                    // So all we want to send is the message key and the read proof nonce and the requesting node
                    // Can use their local data to validate the nonce

                    if (sendPruned)
                        entry = entry.Join(BitShifter.ToByte((ushort)0));
                    else
                        entry = entry
                            .Join(BitShifter.ToByte((ushort)m.Value.Data.Length))
                            .Join(m.Value.Data);

                    // Adding this entry would place the message length above the maximum
                    // So we send what we have and then start a new message
                    if (payload.Count + entry.Count > ushort.MaxValue)
                    {
                        List<byte> data = Header.Create(CODE, false, (ushort)payload.Count)
                            .Join(payload);

                        var ok = await c.WriteAsync(data.ToArray()).ConfigureAwait(false);

                        if (!ok)
                        {
                            await ConnectionManager.RemoveAsync(c, "Not connected").ConfigureAwait(false);
                            return;
                        }

                        payload.Clear();
                        payload.AddRange(entry);
                    }
                    else
                        payload.AddRange(entry);
                }

                // Send any remaining data
                if (payload.Count > 0)
                {
                    List<byte> data = Header.Create(CODE, false, (ushort)payload.Count)
                        .Join(payload);

                    var ok = await c.WriteAsync(data.ToArray()).ConfigureAwait(false);
                    if (!ok)
                        await ConnectionManager.RemoveAsync(c, "Not connected").ConfigureAwait(false);
                }
            }
            else
            {
                lockObject.Wait();

                try
                {
                    int bytesRead = 0;
                    int newMessageCount = 0;
                    BinaryReader reader = new BinaryReader(new MemoryStream(d));

                    while (true)
                    {
                        if (bytesRead >= h.DataLength)
                            break;

                        HashKey16 messageKey = reader.ReadBytes(16);
                        HashKey16 readProofNonce = reader.ReadBytes(16);

                        ushort messageLength = BitShifter.ToUShort(reader.ReadBytes(2));
                        byte[] messageBody = null;

                        if (messageLength > 0)
                            messageBody = reader.ReadBytes(messageLength);

                        if (d.Length < messageLength + bytesRead)
                        {
                            await c.AddFailureAsync("RequestMessagePool: Incomplete message").ConfigureAwait(false);
                            continue;
                        }

                        bytesRead += (34 + messageLength);

                        Message msg = null;
                        if (!MessagePool.Messages.TryGetValue(messageKey, out msg))
                        {
                            // This message is not in our local pool.
                            if (messageBody == null || messageBody.Length != messageLength)
                            {
                                // We were not provided with a full message
                                await c.AddFailureAsync("RequestMessagePool: Incomplete message").ConfigureAwait(false);
                                continue;
                            }

                            if (!messageBody.Validate(false, out msg))
                            {
                                // Message failed validation
                                await c.AddFailureAsync("RequestMessagePool: Message failed validation").ConfigureAwait(false);
                                continue;
                            }

                            if (msg.Key != messageKey)
                            {
                                //The calculated key does not match the provided key
                                await c.AddFailureAsync("RequestMessagePool: Message failed validation").ConfigureAwait(false);
                                continue;
                            }

                            //Don't add an message to the pool if it is already expired
                            if (msg.IsExpired())
                                continue;

                            //Helpers.MessageAll(ShareMessage.GenerateRequest(true, messageBody).ToArray());

                            if (MessagePool.Messages.TryAdd(messageKey, msg))
                            {
                                KeyRing.DecryptMessage(ref msg);

                                ++newMessageCount;
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

                                //validate the read proof and update if necessary
                                if (readProofNonce != HashKey16.Empty)
                                {
                                    HashKey32 readProofHash = ReadProof.GenerateHash(readProofNonce);

                                    if (readProofHash != msg.ExtractReadProofHash())
                                    {
                                        await c.AddFailureAsync("RequestMessagePool: Read proof failed validation").ConfigureAwait(false);
                                        continue;
                                    }

                                    // Read proof passed verification. Add it to the message
                                    msg.ReadProof = new ReadProof
                                    {
                                        Nonce = readProofNonce,
                                        Hash = readProofHash,
                                        IsRead = true
                                    };
                                }
                            }
                        }
                    }

                    reader.Close();
                }
                finally { lockObject.Release(); }
            }
        }
    }
}