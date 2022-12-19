using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Serializer;

namespace EMS
{
    public static class Config
    {
        //Set a future time limit to prevent people extending the message life by using a time in the future
        public const uint FTL = 300;

        //Difficulty for the PoW hash of the message is set at (expiration time) * (multiplier)
        public const uint DIFF_MULTIPLIER = 1024;

        //Give messages a minimum life and enforce it to prevent spamming the network with short lived, low diff messages
        public const uint MIN_MESSAGE_EXPIRATION = 3600;

        public const byte MESSAGE_VERSION = 0;

        public static readonly string DEFAULT_DATA_DIR = Path.Combine(Helpers.HomeDirectory, "EMS");

        public static readonly string DEFAULT_CONFIG_FILE_NAME = "app.config";

        public static readonly string DEFAULT_KEYRING_NAME = "default";

        public const ushort DEFAULT_P2P_PORT = 3500;
        public const ushort DEFAULT_RPC_PORT = 4500;
        public const ushort DEFAULT_KEYRING_COUNT = 10;

        private static UserConfig user = null;

        private static string userConfigFile;

        public static UserConfig User => user;

        public static void Initialize(string file)
        {
            userConfigFile = file;

            if (!File.Exists(file))
                user = new UserConfig();
            else
                user = new ObjectSerializer().Deserialize<UserConfig>(XDocument.Load(file));

            user.InitializePaths();
        }

        public static void Save()
        {
            if (!Directory.Exists(user.DataDir))
                Directory.CreateDirectory(user.DataDir);

            new ObjectSerializer().Serialize(user, userConfigFile);
        }
    }

    //Attribute to designate which properties can be used as CLI flags
    public class CommandLinePropertyAttribute : Attribute
    {
        private string flag;
        private string description;

        public string Flag => flag;
        public string Description => description;

        public CommandLinePropertyAttribute(string flag, string description)
        {
            this.flag = flag;
            this.description = description;
        }
    }

    //Maps the CLI flags to the config file properties
    public static class ConfigMapper
    {
        //The extra list is for command line options you want to show in the help list
        // as valid command line options, but don't want to save in the Config file
        public static string[,] ExtraList = new string[,]
        {
            {"password", "Key file password. Omit to be prompted for a password"},
            {"config-file", "Path to an existing config file to load"}
        };

        private static bool ExtraListContains(string flag)
        {
            for (int i = 0; i < ExtraList.GetLength(0); i++)
                if (ExtraList[i, 0] == flag)
                    return true;
            
            return false;
        }

        private static Dictionary<string, Tuple<CommandLinePropertyAttribute, PropertyInfo>> map = 
            new Dictionary<string, Tuple<CommandLinePropertyAttribute, PropertyInfo>>();

        public static bool Process(Arguments arguments)
        {
            foreach (var p in typeof(UserConfig).GetProperties())
            {
                CommandLinePropertyAttribute a = p.GetCustomAttributes(true).OfType<CommandLinePropertyAttribute>().FirstOrDefault();
                if (a == null || !p.CanWrite)
                    continue;

                map.Add(a.Flag, new Tuple<CommandLinePropertyAttribute, PropertyInfo>(a, p));
            }

            while (arguments.Count > 0)
            {
                Argument arg = arguments.Pop();
                if (arg.Flag == null)
                    continue;

                if (arg.Flag == "help")
                {
                    ShowHelp();
                    return false;
                }

                if (ExtraListContains(arg.Flag))
                    continue;
                
                //provided a flag wrong
                if (!map.ContainsKey(arg.Flag))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"No flag matches {arg.Flag}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ShowHelp();
                    return false;
                }

                var dat = map[arg.Flag];

                //boolean flags do not need a value
                if (dat.Item2.PropertyType == typeof(bool))
                {
                    dat.Item2.SetValue(Config.User, true);
                    continue;
                }

                //check we have a value if the flag expects a value
                if (string.IsNullOrEmpty(arg.Value))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"No value provided for flag {arg.Flag}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ShowHelp();
                    return false;
                }

                if (!Parse(dat, arg))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Could not parse value for flag {arg.Flag}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ShowHelp();
                    return false;
                }
            }

            return true;
        }

        private static bool Parse(Tuple<CommandLinePropertyAttribute, PropertyInfo> dat, Argument arg)
        {
            if (dat.Item2.PropertyType.IsGenericType)
            {
                try
                {
                    object obj = Serializer.Deserialize(dat.Item2.PropertyType.GenericTypeArguments[0], arg.Value);
                    object instance = dat.Item2.GetValue(Config.User);
                    dat.Item2.PropertyType.GetMethod("Add").Invoke(instance, new object[] { obj });
                    return true;
                }
                catch { return false; }
            }
            else
            {
                try
                {
                    object obj = Serializer.Deserialize(dat.Item2.PropertyType, arg.Value);
                    dat.Item2.SetValue(Config.User, obj);
                    return true;
                }
                catch { return false; }
            }
        }

        private static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("EMS command line help");

            for (int i = 0; i < ExtraList.GetLength(0); i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{("--" + ExtraList[i, 0]).PadLeft(16)}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($": {ExtraList[i, 1]}");
            }

            foreach (var i in map.Values)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{("--" + i.Item1.Flag).PadLeft(16)}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($": {i.Item1.Description}");
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    //Config file which we can use for CLI flags
    public class UserConfig
    {
        private Paths paths = null;

        [CommandLineProperty("expiration", "Set the expiration time of messages (seconds).")]
        public uint MessageExpiration { get; set; } = Config.MIN_MESSAGE_EXPIRATION;

        [CommandLineProperty("p2p-port", "P2P port. Default 3500")]
        public ushort P2pPort { get; set; } = Config.DEFAULT_P2P_PORT;

        [CommandLineProperty("rpc-port", "RPC port. Default 4500")]
        public ushort RpcPort { get; set; } = Config.DEFAULT_RPC_PORT;

        [CommandLineProperty("no-dns-seeds", "Do not fetch seed nodes from DNS")]
        public bool NoDnsSeeds { get; set; } = false;

        [CommandLineProperty("relay-only", "Use this node only for relaying messages. Does not open a key file.")]
        public bool RelayOnly { get; set; } = false;

        [CommandLineProperty("no-user-input", "Restrict node to not accept user input.")]
        public bool NoUserInput { get; set; } = false;

        [CommandLineProperty("cache-incoming", "Save incoming messages to file automatically.")]
        public bool CacheIncoming { get; set; } = false;
        
        [CommandLineProperty("seed-nodes", "; delimited list of additional seed nodes")]
        public string SeedNodes { get; set; } = string.Empty;

        [CommandLineProperty("num-keys", "Number of keys to use in your key ring.")]
        public int NumKeys { get; set; } = Config.DEFAULT_KEYRING_COUNT;

        [CommandLineProperty("autoread", "Automatically display incoming messages.")]
        public bool AutoreadMessages { get; set; } = true;

        [CommandLineProperty("data-dir", "Director to store all EMS related data.")]
        public string DataDir { get; set; } = Config.DEFAULT_DATA_DIR;

        [CommandLineProperty("keyring", "The name of the key ring to use.")]
        public string KeyRing { get; set; } = Config.DEFAULT_KEYRING_NAME;

        [CommandLineProperty("name", "Your name or alias. This is shown to other users you send your contact details to.")]
        public string ClientName { get; set; } = null;

        [CommandLineProperty("net-id", "The ID of the network you want to conect to.")]
        public int NetID { get; set; } = 0;

        [CommandLineProperty("seed-dns", "Domain name to fetch seed node data from.")]
        public string SeedDns { get; set; } = string.Empty;

        public Paths Paths => paths;

        public void InitializePaths()
        {
            paths = new Paths(DataDir, KeyRing);
        }
    }

    public class Paths
    {
        private string keyFile;
        private string cacheFile;

        public string KeyFile => keyFile;

        public string CacheFile => cacheFile;

        public Paths(string dataDir, string name)
        {
            keyFile = Path.Combine(dataDir, $"{name}.keys");
            cacheFile = Path.Combine(dataDir, $"{name}.cache");
        }
    }
}