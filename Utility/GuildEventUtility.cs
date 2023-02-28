using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public static class GuildEventUtility
    {
        public static string CreateEventLink(ulong guildId, ulong discordEventId)
        {
            return $"https://discord.com/events/{guildId}/{discordEventId}";
        }

        public static bool TryParseEventDetails(string text, out ulong guildId, out ulong discordEventId)
        {
            guildId = discordEventId = 0;
            var match = parser.Match(text);
            return match.Success
                && ulong.TryParse(match.Groups[1].Value, out guildId)
                && ulong.TryParse(match.Groups[2].Value, out discordEventId);
        }

        internal static string CreateEventDescription(List<string> reactionUsers, string previousDescription)
        {
            var sb = new StringBuilder();
            if (!previousDescription.Contains(Delimiter))
                sb.Append(previousDescription);
            else
            {
                var parts = previousDescription.Split(Delimiter);
                foreach (var part in parts.Take(parts.Length - 1))
                    sb.Append(part);
            }

            sb.AppendLine(Delimiter);
            foreach (var user in reactionUsers)
                sb.AppendLine($"{user} N/A");

            return sb.ToString();
        }

        public const string Delimiter = "----------------------";

        private static readonly Regex parser = new(@"https:\/\/discord\.com\/events\/(\d+)\/(\d+)", RegexOptions.Compiled);
    }
}
