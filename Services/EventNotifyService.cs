using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules;
using Jekbot.Utility;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Services;

[AutoDiscoverSingletonService, ForceInitialization]
public class EventNotifyService
{
    public EventNotifyService(DiscordSocketClient discord, SchedulerService scheduler, ILogger<EventNotifyService> logger)
    {
        this.discord = discord;
        discord.GuildScheduledEventCreated += Discord_GuildScheduledEventCreated;
        discord.GuildScheduledEventCancelled += Discord_GuildScheduledEventCancelled;
        discord.GuildScheduledEventUpdated += Discord_GuildScheduledEventUpdated;
        discord.Connected += Discord_Connected;
        discord.JoinedGuild += Discord_JoinedGuild;
        this.scheduler = scheduler;
        this.logger = logger;
    }

    private async Task Discord_Connected()
    {
        using var tran = scheduler.BeginTransaction();

        ClearEventJobs();
        foreach (var guild in discord.Guilds)
            await Discord_JoinedGuild(guild);
    }

    private void ClearEventJobs()
    {
        using var tran = scheduler.BeginTransaction();

        var liveEvents = guildEventToLiveEvent.ToList();
        foreach (var (key, value) in liveEvents)
        {
            scheduler.RemoveJob(value.JobHandle);
            guildEventToLiveEvent.Remove(key, out _);
        }
    }

    private async Task Discord_JoinedGuild(SocketGuild guild)
    {
        using var tran = scheduler.BeginTransaction();

        foreach (var e in await guild.GetEventsAsync())
        {
            guildEventToLiveEvent.TryAdd(e.Id, new LiveEvent(
                scheduler.AddJob(e.StartTime.ToInstant() - Duration.FromMinutes(5), () => NotifyEvent(e.Guild.Id, e.Id)),
                e.Id,
                guild.Id,
                e.StartTime.ToInstant())
            );
        }
    }

    private Task Discord_GuildScheduledEventUpdated(Cacheable<SocketGuildEvent, ulong> cachedEvent, SocketGuildEvent guildEvent)
    {
        if (!guildEventToLiveEvent.TryGetValue(cachedEvent.Value.Id, out var oldEvent))
        {
            Discord_GuildScheduledEventCreated(guildEvent);
            return Task.CompletedTask;
        }

        var newEvent = oldEvent with { EventTime = guildEvent.StartTime.ToInstant() - Duration.FromMinutes(5) };
        guildEventToLiveEvent.TryRemove(oldEvent.JobHandle, out _);
        guildEventToLiveEvent.TryAdd(newEvent.JobHandle, newEvent);

        if (oldEvent.EventTime != newEvent.EventTime)
            scheduler.UpdateJob(newEvent.JobHandle, newEvent.EventTime, () => NotifyEvent(newEvent.GuildId, newEvent.Id));

        return Task.CompletedTask;
    }

    private Task Discord_GuildScheduledEventCancelled(SocketGuildEvent arg)
    {
        if (guildEventToLiveEvent.TryRemove(arg.Id, out var liveEvent))
            scheduler.RemoveJob(liveEvent.JobHandle);

        return Task.CompletedTask;
    }

    private Task Discord_GuildScheduledEventCreated(SocketGuildEvent arg)
    {
        guildEventToLiveEvent[arg.Id] = new LiveEvent(
            scheduler.AddJob(arg.StartTime.ToInstant() - Duration.FromMinutes(5), () => NotifyEvent(arg.Guild.Id, arg.Id)),
            arg.Guild.Id,
            arg.Id,
            arg.StartTime.ToInstant()
        );

        return Task.CompletedTask;
    }

    private async Task NotifyEvent(ulong guildId, ulong eventId)
    {
        var instance = Instance.Get(guildId);
        if (!instance.IsFeatureEnabled(FeatureId.EventNotify))
            return;

        if (instance.Database.Select<HandledEvent>()
            .Where(x => x.DiscordEventId == eventId)
            .Any()) return;

        var guild = discord.GetGuild(guildId);
        var guildEvent = await guild.GetEventAsync(eventId);
        if (guildEvent == null)
            throw new Exception($"Failed to download guildEvent {eventId}");

        var config = instance.Database.GetSingleton<EventNotifyModule.Config>().Value;
        if (config.ChannelId is not ulong channelId)
        {
            logger.LogWarning("EventNotify channel is not configured");
            return;
        }

        var channel = guild.GetTextChannel(channelId);
        if (channel == null)
        {
            logger.LogWarning("EventNotify channel could not be loaded");
            return;
        }

        var users = await guildEvent
            .GetUsersAsync()
            .FlattenAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Starting soon!");
        sb.AppendLine(string.Join(',', users.Select(x => x.Mention)));
        sb.AppendLine(GuildEventUtility.CreateEventLink(guild.Id, guildEvent.Id));
        await channel.SendMessageAsync(sb.ToString());

        using (var s = instance.Database.BeginSession())
            s.Insert(new HandledEvent { DiscordEventId = eventId });

        if (guildEvent.Status == GuildScheduledEventStatus.Scheduled)
            await guildEvent.StartAsync();
    }

    private readonly DiscordSocketClient discord;
    private readonly ILogger<EventNotifyService> logger;
    private readonly ConcurrentDictionary<ulong, LiveEvent> guildEventToLiveEvent = new();
    private readonly SchedulerService scheduler;

    private record class LiveEvent(ulong JobHandle, ulong Id, ulong GuildId, Instant EventTime);
}
