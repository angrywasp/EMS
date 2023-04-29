using System.IO;
using System.Xml.Linq;
using AngryWasp.Cli;
using AngryWasp.Cli.Config;
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

        public const string DEFAULT_CONFIG_FILE_NAME = "app.config";

        public const string DEFAULT_KEYRING_NAME = "default";

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

    //Config file which we can use for CLI flags
    public class UserConfig
    {
        private Paths paths = null;

        [CommandLineArgument("expiration", Config.MIN_MESSAGE_EXPIRATION, "Set the expiration time of messages (seconds).")]
        public uint MessageExpiration { get; set; }

        [CommandLineArgument("p2p-port", Config.DEFAULT_P2P_PORT, "P2P port. Default 3500")]
        public ushort P2pPort { get; set; }

        [CommandLineArgument("rpc-port", Config.DEFAULT_RPC_PORT, "RPC port. Default 4500")]
        public ushort RpcPort { get; set; }

        [CommandLineArgument("no-dns-seeds", false, "Do not fetch seed nodes from DNS")]
        public bool NoDnsSeeds { get; set; }

        [CommandLineArgument("relay-only", false, "Use this node only for relaying messages. Does not open a key file.")]
        public bool RelayOnly { get; set; }

        [CommandLineArgument("no-user-input", false, "Restrict node to not accept user input.")]
        public bool NoUserInput { get; set; }

        [CommandLineArgument("cache-incoming", false, "Save incoming messages to file automatically.")]
        public bool CacheIncoming { get; set; }
        
        [CommandLineArgument("seed-nodes", null, "; delimited list of additional seed nodes")]
        public string SeedNodes { get; set; }

        [CommandLineArgument("num-keys", Config.DEFAULT_KEYRING_COUNT, "Number of keys to use in your key ring.")]
        public int NumKeys { get; set; }

        [CommandLineArgument("autoread", true, "Automatically display incoming messages.")]
        public bool AutoreadMessages { get; set; }

        [CommandLineArgument("data-dir", null, "Director to store all EMS related data.")]
        public string DataDir { get; set; } = Config.DEFAULT_DATA_DIR;

        [CommandLineArgument("keyring", Config.DEFAULT_KEYRING_NAME, "The name of the key ring to use.")]
        public string KeyRing { get; set; }

        [CommandLineArgument("name", null, "Your name or alias. This is shown to other users you send your contact details to.")]
        public string ClientName { get; set; }

        [CommandLineArgument("net-id", 0, "The ID of the network you want to conect to.")]
        public int NetID { get; set; }

        [CommandLineArgument("seed-dns", null, "Domain name to fetch seed node data from.")]
        public string SeedDns { get; set; }

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