using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Services.ReactionHandlers;

internal class PinMessageHandler : IReactionHandler
{
    public IEmote Emote { get; } = new Emoji("📌");

    public async Task OnReactionAdded(IUserMessage message, IMessageChannel channel, SocketReaction reaction, DiscordSocketClient discord)
    {
        if (GetAllowedActions(channel, reaction).HasFlag(AllowedAction.Pin) && !message.IsPinned)
            await message.PinAsync();
    }

    public async Task OnReactionRemoved(IUserMessage message, IMessageChannel channel, SocketReaction reaction, DiscordSocketClient discord)
    {
        if (GetAllowedActions(channel, reaction).HasFlag(AllowedAction.Unpin) && message.IsPinned)
            await message.UnpinAsync();
    }

    private static AllowedAction GetAllowedActions(IMessageChannel channel, SocketReaction reaction)
    {
        if (channel is not IGuildChannel gc)
            return AllowedAction.None;

        var instance = Instance.Get(gc.GuildId);
        if (!instance.IsFeatureEnabled(FeatureId.UserPins))
            return AllowedAction.None;

        return channel is IThreadChannel tc && tc.OwnerId == reaction.UserId ?
            AllowedAction.All : AllowedAction.None;
    }

    private enum AllowedAction
    {
        None = 0,
        Pin = 1,
        Unpin = 2,
        All = Pin | Unpin,
    }
}
