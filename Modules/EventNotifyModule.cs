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

            using var config = Context.GetInstance().Database.GetSingleton<Config>();
            config.Value.ChannelId = channel.Id;

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"Event notifications will now be posted to {channel.Mention}")
                .Build());
        }
    }

    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);
        discord.GuildScheduledEventCreated += Discord_GuildScheduledEventCreated;
        discord.GuildScheduledEventCancelled += Discord_GuildScheduledEventCancelled;
        discord.GuildScheduledEventUpdated += Discord_GuildScheduledEventUpdated;
        discord.Ready += Discord_Ready;

        Task.Run(Spin);
    }

    private async Task Discord_Ready()
    {
        events.Clear();
        foreach (var guild in discord.Guilds)
        {
            foreach (var e in await guild.GetEventsAsync())
                events.TryAdd(e.Id, new TrackedEvent(e.Id, guild.Id, e.StartTime.ToInstant()));
        }

        signal.Release();
    }

    private async Task Spin()
    {
        while (true)
        {
            await NextTick();

            var processEvents = events
                .ToDictionary(x => x.Key, x => x.Value);

            var now = SystemClock.Instance.GetCurrentInstant();
            foreach (var (k, v) in processEvents)
            {
                if (v.FiveMinuteWarning < now )
                {
                    await NotifyEvent(v);
                    events.TryRemove(k, out _);
                }
            }
        }
    }

    private async Task NotifyEvent(TrackedEvent v)
    {
        var instance = Instance.Get(v.GuildId);
        if (!instance.IsFeatureEnabled(FeatureId.EventNotify)) return;

        var guild = discord.GetGuild(v.GuildId);
        var guildEvent = await guild.GetEventAsync(v.Id);

        var config = instance.Database.GetSingleton<Config>().Value;
        if (guildEvent != null && config.ChannelId is ulong channelId)
        {
            var channel = guild.GetTextChannel(channelId);
            if (channel != null)
            {
                var users = await guildEvent
                    .GetUsersAsync()
                    .FlattenAsync();

                var sb = new StringBuilder();
                sb.AppendLine("Starting soon!");
                sb.AppendLine(string.Join(',', users.Select(x => x.Mention)));
                sb.AppendLine(GuildEventUtility.CreateEventLink(guild.Id, guildEvent.Id));

                await channel.SendMessageAsync(sb.ToString());
                await guildEvent.StartAsync();
            }
        }
    }

    private async Task NextTick()
    {
        await Task.WhenAny(
            signal.WaitAsync(),
            Task.Delay(ApproachNextEvent())
        );

        if (signal.CurrentCount != 0)
            await signal.WaitAsync();
    }

    private TimeSpan ApproachNextEvent()
    {
        if (!events.Any())
            return Timeout.InfiniteTimeSpan;

        var now = SystemClock.Instance.GetCurrentInstant();
        var earliest = events.Values
            .Min(x => x.FiveMinuteWarning);

        if (earliest < now)
            return TimeSpan.Zero;
        
        var half = (earliest - now) / 2;
        if (half < Duration.FromMinutes(5))
            return TimeSpan.FromSeconds(5);
        else return half.ToTimeSpan();
    }

    private Task Discord_GuildScheduledEventUpdated(Cacheable<SocketGuildEvent, ulong> cachedEvent, SocketGuildEvent guildEvent)
    {
        if (!events.TryGetValue(cachedEvent.Value.Id, out var oldEvent))
        {
            Discord_GuildScheduledEventCreated(guildEvent);
            return Task.CompletedTask;
        }

        var newEvent = oldEvent with { EventTime = guildEvent.StartTime.ToInstant() };
        events.TryRemove(oldEvent.Id, out _);
        events.TryAdd(newEvent.Id, newEvent);

        if (oldEvent.EventTime != newEvent.EventTime)
            signal.Release();

        return Task.CompletedTask;
    }

    private Task Discord_GuildScheduledEventCancelled(SocketGuildEvent arg)
    {
        events.TryRemove(arg.Id, out _);
        signal.Release();
        return Task.CompletedTask;
    }

    private Task Discord_GuildScheduledEventCreated(SocketGuildEvent arg)
    {
        events.TryAdd(arg.Id, new TrackedEvent(arg.Id, arg.Guild.Id, arg.StartTime.ToInstant()));
        signal.Release();
        return Task.CompletedTask;
    }

    public EventNotifyModule(DiscordSocketClient discord, ILogger<EventNotifyModule> logger)
    {
        this.discord = discord;
        this.logger = logger;
    }

    private readonly DiscordSocketClient discord;
    private readonly ILogger<EventNotifyModule> logger;
    private readonly ConcurrentDictionary<ulong, TrackedEvent> events = new();
    private readonly SemaphoreSlim signal = new(0, 1);

    private record class TrackedEvent(ulong Id, ulong GuildId, Instant EventTime)
    {
        public Instant FiveMinuteWarning => EventTime - Duration.FromMinutes(5);
    }
}
