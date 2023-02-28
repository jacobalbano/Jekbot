using Discord;
using Discord.WebSocket;
using Jekbot.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems.ReactionHandlers
{
    internal class RsvpAwayHandler : IReactionHandler
    {
        public IEmote Emote { get; } = new Emoji("❌");

        public Task OnReactionAdded(IUserMessage message, IMessageChannel channel, SocketReaction reaction, DiscordSocketClient discord)
        {
            return UpdateEventDetails(message, channel, reaction, discord);
        }

        public Task OnReactionRemoved(IUserMessage message, IMessageChannel channel, SocketReaction reaction, DiscordSocketClient discord)
        {
            return UpdateEventDetails(message, channel, reaction, discord);
        }

        private async Task UpdateEventDetails(IUserMessage message, IMessageChannel channel, IReaction reaction, DiscordSocketClient discord)
        {
            if (!GuildEventUtility.TryParseEventDetails(message.CleanContent, out var guildId, out var eventId))
                return;

            if (channel is not IGuildChannel gc || guildId != gc.GuildId)
                return;

            var guildEvent = await gc.Guild.GetEventAsync(eventId);
            if (guildEvent == null) return;

            var reactionUsers = (await message.GetReactionUsersAsync(Emote, 100)
                .FlattenAsync())
                .Where(x => !x.IsBot)
                .Select(x => x.Mention)
                .ToList();

            await guildEvent.ModifyAsync(props => props.Description = GuildEventUtility.CreateEventDescription(reactionUsers, guildEvent.Description ?? string.Empty));
        }
    }
}
