using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems.ReactionHandlers
{
    public interface IReactionHandler
    {
        public IEmote Emote { get; }

        public Task OnReactionAdded(IUserMessage message, IMessageChannel channel, SocketReaction reaction, Discord.WebSocket.DiscordSocketClient discord);
        public Task OnReactionRemoved(IUserMessage message, IMessageChannel channel, SocketReaction reaction, Discord.WebSocket.DiscordSocketClient discord);

        public static IEnumerable<IReactionHandler> DiscoverReactionHandlers() => typeof(IReactionHandler).Assembly
            .GetTypes()
            .Where(typeof(IReactionHandler).IsAssignableFrom)
            .Where(x => !x.IsInterface)
            .Select(x => (IReactionHandler)Activator.CreateInstance(x));
    }
}
