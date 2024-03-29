﻿using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems;

[AutoDiscoverSingletonService]
public class PinService
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
            using var s = instance.Database.BeginSession();

            var lastPin = s.Select<PinnedMessage>()
                .FirstOrDefault(x => x.UniqueName == uniquePinId);

            if (lastPin != null)
            {
                var lastMessage = await GetMessage(guild, lastPin.DiscordChannelId, lastPin.DiscordMessageId);
                if (lastMessage != null && lastMessage.IsPinned)
                    await lastMessage.UnpinAsync();

                s.Delete(lastPin);
            }

            s.Insert(new PinnedMessage() { DiscordChannelId = discordChannelId, DiscordMessageId = discordMessageId, UniqueName = uniquePinId });
        }
    }

    private static async Task<IUserMessage?> GetMessage(SocketGuild guild, ulong discordChannelId, ulong discordMessageId)
    {
        var channel = guild.GetTextChannel(discordChannelId);
        if (channel == null)
            return null;

        return await channel.GetMessageAsync(discordMessageId) as IUserMessage;
    }

    public PinService(DiscordSocketClient discord)
    {
        this.discord = discord;
    }

    private readonly DiscordSocketClient discord;
}
