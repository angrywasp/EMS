using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Helpers;
using AngryWasp.Logger;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("get", "Get a config option value. Usage: get <param>")]
    public class GetConfig : IApplicationCommand
    {
        public async Task<bool> Handle(string command)
        {
            string propName = Helpers.PopWord(ref command);
            var props = ReflectionHelper.Instance.GetProperties(typeof(UserConfig), Property_Access_Mode.Read | Property_Access_Mode.Write);


            if (string.IsNullOrEmpty(propName))
            {
                foreach (var p in props)
                    Log.Instance.Write($"{p.Key}: {p.Value.GetValue(Config.User).ToString()}");

                return true;
            }
            else
            {
                foreach (var p in props)
                {
                    if (p.Key == propName)
                    {
                        Log.Instance.Write($"{p.Key}: {p.Value.GetValue(Config.User).ToString()}");
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}