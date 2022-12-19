using AngryWasp.Cli;
using AngryWasp.Helpers;
using AngryWasp.Logger;
using AngryWasp.Serializer;
using System.Reflection;
using System.Threading.Tasks;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("set", "Set a config option value. Usage: set <param> <value>")]
    public class SetConfig : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            string propName = Helpers.PopWord(ref command);
            string propValue = Helpers.PopWord(ref command);

            if (string.IsNullOrEmpty(propName) || string.IsNullOrEmpty(propValue))
            {
                Log.Instance.WriteError("Incorrect number of arguments");
                return false;
            }
            
            var props = ReflectionHelper.Instance.GetProperties(typeof(UserConfig), Property_Access_Mode.Read | Property_Access_Mode.Write);
            PropertyInfo pi = null;

            foreach (var prop in props)
            {
                if (prop.Key == propName)
                {
                    pi = prop.Value;
                    break;
                }
            }

            if (pi == null)
            {
                Log.Instance.WriteError($"Property {propName} is invalid");
                return false;
            }

            object value = Serializer.Deserialize(pi.PropertyType, propValue);
            pi.SetValue(Config.User, value);

            Config.Save();

            Log.Instance.WriteWarning("A restart is required for the changes to take effect.");

            return true;
        }
    }
}