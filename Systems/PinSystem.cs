using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
    [AutoDiscoverSingletonService]
    public class PinSystem
    {
        public async Task PinMessage(Instance instance, ulong discordChannelId, ulong discordMessageId, string uniquePinId = null)
        {
            var guild = discord.GetGuild(instance.Id);
            var message = await GetMessage(guild, discordChannelId, discordMessageId);
            if (message == null || message.IsPinned)
                return;

            await message.PinAsync();

            if (uniquePinId != null)
            {
                var lastPin = instance.Database
                    .Select<PinnedMessage>()
                    .Where(x => x.UniqueName == uniquePinId)
                    .FirstOrDefault();

                if (lastPin != null)
                {
                    var lastMessage = await GetMessage(guild, lastPin.DiscordChannelId, lastPin.DiscordMessageId);
                    if (lastMessage != null && lastMessage.IsPinned)
                        await lastMessage.UnpinAsync();

                    instance.Database.Delete(lastPin);
                }

                instance.Database.Insert<PinnedMessage>(new() { DiscordChannelId = discordChannelId, DiscordMessageId = discordMessageId, UniqueName = uniquePinId });
            }
        }

        private static async Task<IUserMessage?> GetMessage(SocketGuild guild, ulong discordChannelId, ulong discordMessageId)
        {
            var channel = guild.GetTextChannel(discordChannelId);
            if (channel == null)
                return null;

            return await channel.GetMessageAsync(discordMessageId) as IUserMessage;
        }

        public PinSystem(DiscordSocketClient discord)
        {
            this.discord = discord;
        }

        private readonly DiscordSocketClient discord;
    }
}
