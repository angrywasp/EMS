using System.IO;
using System.Xml.Linq;
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

    //Config file which we can use for CLI flags
    public class UserConfig
    {
        private Paths paths = null;

        [CommandLineArgument("expiration", "Set the expiration time of messages (seconds).")]
        public uint MessageExpiration { get; set; } = Config.MIN_MESSAGE_EXPIRATION;

        [CommandLineArgument("p2p-port", "P2P port. Default 3500")]
        public ushort P2pPort { get; set; } = Config.DEFAULT_P2P_PORT;

        [CommandLineArgument("rpc-port", "RPC port. Default 4500")]
        public ushort RpcPort { get; set; } = Config.DEFAULT_RPC_PORT;

        [CommandLineArgument("no-dns-seeds", "Do not fetch seed nodes from DNS")]
        public bool NoDnsSeeds { get; set; } = false;

        [CommandLineArgument("relay-only", "Use this node only for relaying messages. Does not open a key file.")]
        public bool RelayOnly { get; set; } = false;

        [CommandLineArgument("no-user-input", "Restrict node to not accept user input.")]
        public bool NoUserInput { get; set; } = false;

        [CommandLineArgument("cache-incoming", "Save incoming messages to file automatically.")]
        public bool CacheIncoming { get; set; } = false;
        
        [CommandLineArgument("seed-nodes", "; delimited list of additional seed nodes")]
        public string SeedNodes { get; set; } = string.Empty;

        [CommandLineArgument("num-keys", "Number of keys to use in your key ring.")]
        public int NumKeys { get; set; } = Config.DEFAULT_KEYRING_COUNT;

        [CommandLineArgument("autoread", "Automatically display incoming messages.")]
        public bool AutoreadMessages { get; set; } = true;

        [CommandLineArgument("data-dir", "Director to store all EMS related data.")]
        public string DataDir { get; set; } = Config.DEFAULT_DATA_DIR;

        [CommandLineArgument("keyring", "The name of the key ring to use.")]
        public string KeyRing { get; set; } = Config.DEFAULT_KEYRING_NAME;

        [CommandLineArgument("name", "Your name or alias. This is shown to other users you send your contact details to.")]
        public string ClientName { get; set; } = null;

        [CommandLineArgument("net-id", "The ID of the network you want to conect to.")]
        public int NetID { get; set; } = 0;

        [CommandLineArgument("seed-dns", "Domain name to fetch seed node data from.")]
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