using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Cli.Prompts;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Net;
using DnsClient;

namespace EMS
{
    public static class Helpers
    {
        public static string PopWord(ref string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            int index = input.IndexOf(' ');
            if (index == -1)
            {
                string ret = input;
                input = string.Empty;
                return ret;
            }
            else
            {
                string ret = input.Substring(0, index);
                input = input.Remove(0, ret.Length).TrimStart();
                return ret;
            }
        }

        public static void AddSeedFromDns(int netId, string seedDns)
        {
            if (string.IsNullOrEmpty(seedDns))
                return;

            var client = new LookupClient();
            var records = client.Query($"{netId}.{seedDns}", QueryType.TXT).Answers;

            foreach (var r in records)
            {
                string txt = ((DnsClient.Protocol.TxtRecord)r).Text.ToArray()[0];

                string[] node = txt.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                string host = node[0];
                ushort port = Config.DEFAULT_P2P_PORT;
                if (node.Length > 1)
                    ushort.TryParse(node[1], out port);

                if (AngryWasp.Net.Config.HasSeedNode(host, port))
                    continue;

                AngryWasp.Net.Config.AddSeedNode(host, port);
                Log.Instance.Write($"Added seed node {host}:{port}");
            }
        }

        public static void WriteMessageToConsole(Message m)
        {
            Application.LogBufferPaused = true;
            string dir = m.Direction.ToString().PadLeft(6);
            Console.ForegroundColor = m.Direction == Message_Direction.Out ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen;
            Console.WriteLine($"   Key: {m.Key.ToString()}");
            string id = "unknown";
            if (AddressBook.ReverseLookup.ContainsKey(m.Address))
                id = AddressBook.ReverseLookup[m.Address];

            switch (m.Direction)
            {
                case Message_Direction.Out:
                    Console.WriteLine($"  From: Key {m.DecryptionData.KeyIndex.ToString().PadLeft(2)}, {m.DecryptionData.Address}");
                    Console.WriteLine($"    To: {id} ({m.Address})");
                    break;
                case Message_Direction.In:
                    Console.WriteLine($"  From: {id} ({m.Address})");
                    Console.WriteLine($"    To: Key {m.DecryptionData.KeyIndex.ToString().PadLeft(2)}, {m.DecryptionData.Address}");
                    break;
            }

            Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Timestamp)}");
            Console.WriteLine($"  Type: {m.MessageType} (Version {m.MessageVersion})");
            Console.WriteLine($"Expiry: {DateTimeHelper.UnixTimestampToDateTime(m.Timestamp + m.Expiration)}");
            Console.ForegroundColor = m.Direction == Message_Direction.Out ? ConsoleColor.Magenta : ConsoleColor.Green;
            Console.WriteLine();
            m.ParseDecryptedMessage();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Application.LogBufferPaused = false;
        }

        public static async Task MessageAll(byte[] request)
        {
            List<Connection> disconnected = new List<Connection>();

            await ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, async (c) =>
            {
                var ok = await c.WriteAsync(request).ConfigureAwait(false);
                if (!ok)
                    disconnected.Add(c);
            }).ConfigureAwait(false);

            foreach (var c in disconnected)
                await ConnectionManager.RemoveAsync(c, "Not responding");
        }

        public static string DisplayPasswordPrompt()
        {
            Application.LogBufferPaused = true;

            while (true)
            {
                PasswordPrompt.Get(out string a, "Enter a password for your key file");
                PasswordPrompt.Get(out string b, "Confirm your password");

                if (a != b)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Password do not match. Please try again");
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

                Application.LogBufferPaused = false;
                return a;
            }
        }

        public static bool IsWindows()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    return true;
            }

            return false;
        }

        public static string HomeDirectory
        {
            get => IsWindows() ? Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") : Environment.GetEnvironmentVariable("HOME");
        }

    }
}