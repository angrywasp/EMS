using System;
using AngryWasp.Net;
using EMS.Commands.P2P;
using AngryWasp.Serializer;
using System.Reflection;
using System.IO;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Cli.DefaultCommands;
using AngryWasp.Json.Rpc;
using AngryWasp.Logger;
using AngryWasp.Cli.Config;

namespace EMS
{
    public static class MainClass
    {
        [STAThread]
        public static void Main(string[] rawArgs)
        {
            Arguments args = Arguments.Parse(rawArgs);

            Console.Title = $"EMS {Version.VERSION}: {Version.CODE_NAME}";
            Serializer.Initialize();
            Config.Initialize(args.GetString("config-file", Path.Combine(Config.DEFAULT_DATA_DIR, Config.DEFAULT_CONFIG_FILE_NAME)));
            if (!ConfigMapper<UserConfig>.Process(args, Config.User, new string[,]
            {
                {"password", "Key file password. Omit to be prompted for a password"},
                {"config-file", "Path to an existing config file to load"}
            }))
                return;

            string name = null;
            if (string.IsNullOrEmpty(Config.User.ClientName))
            {
                while (true)
                {
                    name = CliPrompt.UserInput("Please enter you name or alias");
                    if (!string.IsNullOrEmpty(name)) break;
                }

                Config.User.ClientName = name;
            }

            Config.Save();

            Log.CreateInstance(false);
            if (!Config.User.RelayOnly)
                KeyRing.LoadKeyRing(args.GetString("password"));

            new Clear().Handle(null);
            
            CommandProcessor.RegisterCommand("ShareMessage", ShareMessage.CODE, ShareMessage.GenerateResponse);
            CommandProcessor.RegisterCommand("RequestMessagePool", ShareMessageRead.CODE, ShareMessageRead.GenerateResponse);
            CommandProcessor.RegisterCommand("ShareMessageRead", RequestMessagePool.CODE, RequestMessagePool.GenerateResponse);
            CommandProcessor.RegisterDefaultCommands();

            string[] seedNodes = Config.User.SeedNodes.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var seedNode in seedNodes)
            {
                string[] node = seedNode.Split(':', StringSplitOptions.RemoveEmptyEntries);
                string host = node[0];
                ushort port = Config.DEFAULT_P2P_PORT;
                if (node.Length > 1)
                    ushort.TryParse(node[1], out port);

                AngryWasp.Net.Config.AddSeedNode(host, port);
                Log.Instance.Write($"Added seed node {host}:{port}");
            }

            if (!Config.User.NoDnsSeeds)
                Helpers.AddSeedFromDns(Config.User.NetID, Config.User.SeedDns);

            if (Config.User.CacheIncoming)
                File.Create(Config.User.Paths.CacheFile);

            new Server().Start(Config.User.P2pPort, new ConnectionId());

            JsonRpcServer server = new JsonRpcServer(Config.User.RpcPort);
            server.RegisterCommands();
            server.Start();

            Client.ConnectToSeedNodes();

            TimedEventManager.RegisterEvents(Assembly.GetExecutingAssembly());
            Application.RegisterCommands();
            Application.Start();
        }
    }
}
