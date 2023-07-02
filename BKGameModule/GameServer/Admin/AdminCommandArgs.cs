using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Logger;

namespace BKGameServerComponent.GameServer.Admin
{
    public class AdminCommandArgs
    {
        private Dictionary<string, string> args = new Dictionary<string, string>();

        public AdminCommandArgs(string arg)
        {
            foreach (var i in arg.Split('^'))
            {
                var param = i.Split(new char[] { '=' }, StringSplitOptions.TrimEntries);
                if (param.Length != 2)
                {
                    continue;
                }
                args[param[0]] = param[1];
            }
        }

        public string? this[string key]
        {
            get
            {
                if (args.TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }
        }

        //이거 종류별로 다 있어야 됨.
        public int AsInt32(string key, int defaultValue)
        {
            var value = this[key];
            try
            {
                return Convert.ToInt32(value);
            }
            catch 
            {
                CoreLog.Critical.LogWarning($"Invalid Argument. input : {value}");
                return defaultValue; 
            }
        }

        public long AsInt64(string key, int defaultValue)
        {
            var value = this[key];
            try
            {
                return Convert.ToInt64(value);
            }
            catch
            {
                CoreLog.Critical.LogWarning($"Invalid Argument. input : {value}");
                return defaultValue;
            }
        }

        public T AsEnum<T>(string key, T defaultValue)
            where T : struct
        {
            var value = this[key];
            if (Enum.TryParse<T>(value, out var result) == false)
            {
                return defaultValue;
            }
            return result;
        }
    }
}
