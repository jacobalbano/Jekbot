using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
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
                if (instance.PinnedMessages.FirstOrDefault(x => x.UniqueName == uniquePinId) is PinnedMessage lastPin)
                {
                    var lastMessage = await GetMessage(guild, lastPin.DiscordChannelId, lastPin.DiscordMessageId);
                    if (lastMessage != null && lastMessage.IsPinned)
                        await lastMessage.UnpinAsync();

                    instance.PinnedMessages.Remove(lastPin);
                }

                instance.PinnedMessages.Add(new() { DiscordChannelId = discordChannelId, DiscordMessageId = discordMessageId, UniqueName = uniquePinId });
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
