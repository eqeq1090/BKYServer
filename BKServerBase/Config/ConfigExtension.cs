using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Config.Server;
using BKServerBase.ConstEnum;
using BKServerBase.Util;
using BKProtocol;
using BKProtocol.Enum;

namespace BKServerBase.Config
{
    public static class ConfigExtension
    {
        public static void LoadMatchmakingConfigMap(this IConfigurationSection section, out Dictionary<RegionCode, MatchmakingConfigName> matchmakingConfigNameMap)
        {
            matchmakingConfigNameMap = new Dictionary<RegionCode, MatchmakingConfigName>();

            var matchmaking = section.GetSection("Matchmaking");
            if (matchmaking == null)
            {
                throw new InvalidDataException($"Matchmaking section is empty");
            }

            foreach (var matchmakingElement in matchmaking.GetChildren())
            {
                var regionName = matchmakingElement.Key;
                if (Enum.TryParse<RegionCode>(regionName, out var regionType) == false)
                {
                    throw new InvalidDataException($"invalid region type: {regionName}");
                }

                var soloElement = matchmakingElement.GetSection("Solo");
                var soloConfigNames = soloElement.Get<string[]>() ?? throw new Exception($"empty solo config names");

                var squad3vs3Element = matchmakingElement.GetSection("Squad3vs3");
                var squad3vs3ConfigNames = squad3vs3Element.Get<string[]>() ?? throw new Exception($"empty squad3vs3 config names");

                var config = new MatchmakingConfigName(solo: soloConfigNames!, squad3vs3: squad3vs3ConfigNames!);
                matchmakingConfigNameMap.Add(regionType, config);
            }
        }
    }
}
