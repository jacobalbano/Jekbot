using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Modules;

public class EmojiAgreeModule : InteractionModuleBase<SocketInteractionContext>
{
    public EmojiAgreeModule(DiscordSocketClient discord)
    {
        this.discord = discord;
    }

    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);
        discord.ReactionAdded += Discord_ReactionAdded;
    }

    private async Task Discord_ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> message, Discord.Cacheable<Discord.IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        var msg = await message.GetOrDownloadAsync();
        await msg.AddReactionAsync(reaction.Emote);
    }

    private readonly DiscordSocketClient discord;
}
