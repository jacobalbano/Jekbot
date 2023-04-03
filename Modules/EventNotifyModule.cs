using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Utility;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using System.Collections.Concurrent;
using System.Text;

namespace Jekbot.Modules;

public class EventNotifyModule : InteractionModuleBase<SocketInteractionContext>
{
    public record class Config : ModelBase
    {
        public ulong? ChannelId { get; set; }
    }

    [RequireContext(ContextType.Guild)]
    [Group("events", "Event notification")]
    public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("set-channel", "Set the channel to post event notifications")]
        public async Task SetChannel(ITextChannel channel)
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using var config = instance.Database.GetSingleton<Config>();
            config.Value.ChannelId = channel.Id;

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"Event notifications will now be posted to {channel.Mention}")
                .Build());
        }
    }
}
