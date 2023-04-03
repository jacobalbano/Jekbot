using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Services.ReactionHandlers;

internal class ReactionEchoHandler : IReactionHandler
{
    public IEmote Emote => null!;

    public async Task OnReactionAdded(IUserMessage message, IMessageChannel channel, SocketReaction reaction, DiscordSocketClient discord)
    {
        if (channel is not IGuildChannel gc || !Instance.Get(gc.GuildId).IsFeatureEnabled(FeatureId.EmojiAgreement))
            return;

        await message.AddReactionAsync(reaction.Emote);
    }

    public async Task OnReactionRemoved(IUserMessage message, IMessageChannel channel, SocketReaction reaction, DiscordSocketClient discord)
    {
        if (channel is not IGuildChannel gc || !Instance.Get(gc.GuildId).IsFeatureEnabled(FeatureId.EmojiAgreement))
            return;

        if (message.Reactions.TryGetValue(reaction.Emote, out var value))
        {
            if (value.ReactionCount == 1 && value.IsMe)
                await message.RemoveReactionAsync(reaction.Emote, discord.CurrentUser);
        }
    }
}
