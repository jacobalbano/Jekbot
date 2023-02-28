using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Jekbot.Systems.ReactionHandlers;

namespace Jekbot.Modules;

public class ReactionWatcherModule : InteractionModuleBase<SocketInteractionContext>
{
    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);
        discord.ReactionAdded += Discord_ReactionAdded;
        discord.ReactionRemoved += Discord_ReactionRemoved;
    }

    private Task Discord_ReactionRemoved(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        if (!handlers.TryGetValue(reaction.Emote, out var handler))
            handler = defaultHandler;

        return Task.Run(async () =>
        {
            var msgVal = await message.GetOrDownloadAsync();
            var chnlVal = await channel.GetOrDownloadAsync();
            await handler.OnReactionRemoved(msgVal, chnlVal, reaction, discord);
        });
    }

    private Task Discord_ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        if (!handlers.TryGetValue(reaction.Emote, out var handler))
            handler = defaultHandler;

        return Task.Run(async () =>
        {
            var msgVal = await message.GetOrDownloadAsync();
            var chnlVal = await channel.GetOrDownloadAsync();
            await handler.OnReactionAdded(msgVal, chnlVal, reaction, discord);
        });
    }

    public ReactionWatcherModule(DiscordSocketClient discord)
    {
        this.discord = discord;
    }

    private readonly DiscordSocketClient discord;
    private static readonly IReactionHandler defaultHandler = new ReactionEchoHandler();
    private static readonly IReadOnlyDictionary<IEmote, IReactionHandler> handlers = IReactionHandler.DiscoverReactionHandlers()
        .Where(x => x.Emote != null)
        .ToDictionary(x => x.Emote);
}
