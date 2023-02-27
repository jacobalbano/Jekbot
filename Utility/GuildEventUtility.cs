using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public static class GuildEventUtility
    {
        public static string CreateEventLink(ulong guildId, ulong discordEventId)
        {
            return $"https://discord.com/events/{guildId}/{discordEventId}";
        }
    }
}
