using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;

namespace Jekbot.Modules;

public class EmojiAgreeModule : InteractionModuleBase<SocketInteractionContext>
{
    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);
        discord.ReactionAdded += Discord_ReactionAdded;
        discord.ReactionRemoved += Discord_ReactionRemoved;
    }

    private async Task Discord_ReactionRemoved(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        var msg = await message.GetOrDownloadAsync();
        if (msg.Reactions.TryGetValue(reaction.Emote, out var value))
        {
            if (value.ReactionCount == 1 && value.IsMe)
                await msg.RemoveReactionAsync(reaction.Emote, discord.CurrentUser);
        }
    }

    private async Task Discord_ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        var msg = await message.GetOrDownloadAsync();
        await msg.AddReactionAsync(reaction.Emote);
    }

    public EmojiAgreeModule(DiscordSocketClient discord)
    {
        this.discord = discord;
    }

    private readonly DiscordSocketClient discord;
}
