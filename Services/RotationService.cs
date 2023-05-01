using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules;
using Jekbot.Systems;
using Jekbot.Utility;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Services;

public class RotationList : List<RotationEntry>, IDisposable
{
    public RotationList(Instance instance)
    {
        this.instance = instance;
        AddRange(instance.Database
            .Select<RotationEntry>()
            .OrderBy(x => x.Order)
            .ToEnumerable());
    }

    public bool AdvanceRotation()
    {
        if (Count == 0)
            return false;

        AdvancePastSkippedUsers();
        if (this[0].Type == RotationEntryType.User)
            Add(this[0]);

        RemoveAt(0);
        AdvancePastSkippedUsers();
        return true;
    }

    public void AdvancePastSkippedUsers()
    {
        while (this[0].Skip)
        {
            var move = this[0];
            RemoveAt(0);
            Add(move with { Skip = false });
        }
    }

    public void Dispose()
    {
        using var s = instance.Database.BeginSession();

        s.DeleteAll<RotationEntry>();
        for (int i = 0; i < Count; i++)
            s.Insert(this[i] with { Order = i });
    }

    private readonly Instance instance;
}

[AutoDiscoverSingletonService, ForceInitialization]
public class RotationService
{
    public async Task RefreshEvents(Instance instance, RotationList rotation)
    {
        /*
         * Advances past any immediately-skipped users
         * Creates an event for the current rotation if one does not exist
         * Cancels any other events
         */

        if (rotation.Count == 0)
            return;

        rotation.AdvancePastSkippedUsers();

        var next = rotation.First();
        if (next.Type == RotationEntryType.User && next.TrackedEventKey == null)
        {
            var nextEvent = await CreateEventForNextRotation(instance, rotation);
            if (nextEvent != null)
                rotation[0] = rotation[0] with { TrackedEventKey = nextEvent.Key };
        }

        var guild = discord.GetGuild(instance.Id);
        for (int i = 1; i < rotation.Count; i++)
        {
            var rot = rotation[i];
            var key = rot.TrackedEventKey;
            if (rot.TrackedEventKey != null)
            {
                var trackedEvent = instance.Database.Select<TrackedEvent>()
                    .FirstOrDefault(x => x.Key == key);

                if (trackedEvent != null)
                {
                    try
                    {
                        var discordEvent = await guild.GetEventAsync(trackedEvent.DiscordEventId);
                        if (discordEvent != null)
                            await discordEvent.DeleteAsync();

                        using var s = instance.Database.BeginSession();
                        s.Delete(trackedEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to delete event");
                    }
                }

                rotation[i] = rot with { TrackedEventKey = null };
            }
        }
    }

    public async Task<TrackedEvent?> CreateEventForNextRotation(Instance instance, List<RotationEntry> rotation)
    {
        var guild = discord.GetGuild(instance.Id);

        var next = rotation.First();
        if (next.Type != RotationEntryType.User)
            return null;

        var user = await discord.GetUserAsync(next.DiscordUserId ?? throw new Exception("Missing user ID in RotationEntry"));
        var nextGameNight = GenerateFutureGameNightInstants(instance).First();
        var guildEvent = await guild.CreateEventAsync(
            $"Game Night - {user.Username}",
            nextGameNight.ToDateTimeUtc(),
            GuildScheduledEventType.Voice,
            channelId: guild.VoiceChannels.FirstOrDefault()?.Id,
            description: GuildEventUtility.CreateEventDescription(new List<ulong>(), "")
        );

        var nextEvent = new TrackedEvent { DiscordEventId = guildEvent.Id };
        rotation[0] = rotation[0] with { TrackedEventKey = nextEvent.Key };

        using var s = instance.Database.BeginSession();
        return s.Insert(nextEvent);
    }

    public async Task PostRotationMessage(Instance instance, List<RotationEntry> rotation)
    {
        /*
         * Generate a list of dates for each upcoming game night
         * Send a message to the updates channel with a date assigned to each non-skipped user
         * Post a link to the upcoming game night event
         */

        if (rotation.Count == 0)
            return;

        var guild = discord.GetGuild(instance.Id);
        var config = instance.Database.GetSingleton<RotationModule.Config>().Value;
        if (config.ChannelId is ulong channelId && guild.GetChannel(channelId) is ITextChannel channel)
        {
            using var intervals = GenerateFutureGameNightInstants(instance)
                .GetEnumerator();

            var sb = new StringBuilder();
            sb.AppendLine("Current rotation:");
            bool firstUser = true;
            foreach (var item in rotation)
            {
                switch (item.Type)
                {
                    case RotationEntryType.User:
                        if (item.DiscordUserId is not ulong userId)
                            throw new Exception("RotationEntry of type User did not have a user id");

                        var user = await discord.GetUserAsync(userId);
                        if (firstUser)
                        {
                            firstUser = false;
                            sb.Append(":star: ");
                        }

                        sb.Append($"{user.Mention} ");
                        if (item.Skip) sb.AppendLine("(away)");
                        else
                        {
                            intervals.MoveNext();
                            sb.AppendLine($"(<t:{intervals.Current.ToUnixTimeSeconds()}>)");
                        }
                        break;
                    case RotationEntryType.Postponment:
                        intervals.MoveNext();
                        sb.AppendLine($"**Postponed** (<t:{intervals.Current.ToUnixTimeSeconds()}>)");
                        break;
                    default:
                        throw new Exception("Unhandled enum value");
                }
            }

            var next = rotation.FirstOrDefault() ?? throw new Exception("Rotation had no entries!");
            if (next.Type == RotationEntryType.User)
            {
                var trackedEvent = instance.Database
                    .Select<TrackedEvent>()
                    .FirstOrDefault(x => x.Key == next.TrackedEventKey);

                if (trackedEvent != null)
                {
                    sb.AppendLine("Please click 'Interested' or react with ❌ to show your availability!");
                    sb.AppendLine(GuildEventUtility.CreateEventLink(instance.Id, trackedEvent.DiscordEventId));
                }
            }

            var message = await channel.SendMessageAsync(sb.ToString());
            await message.AddReactionAsync(new Emoji("❌"));

            try
            {
                await pins.PinMessage(instance, channel.Id, message.Id, "Rotation");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to pin message");
                await message.ReplyAsync(embed: new EmbedBuilder()
                    .WithDescription("Failed to pin message")
                    .WithColor(Color.Red)
                    .Build());
            }
        }
    }

    public IEnumerable<Instant> GenerateFutureGameNightInstants(Instance instance)
    {
        var config = instance.Database.GetSingleton<RotationModule.Config>().Value;
        if (!config.IsConfigured())
            yield break;

        var tz = timezoneProvider.Tzdb[config.SchedulingRelativeToTz!];
        var clock = SystemClock.Instance.InZone(tz);

        var isoDay = config.ScheduledDay!.Value.ToIsoDayOfWeek();
        var getNextDay = DateAdjusters.NextOrSame(isoDay);
        var time = TimeOnly.Parse(config.ScheduledTime!).ToLocalTime();

        var nextDay = clock.GetCurrentDate();
        while (true)
        {
            var testDay = getNextDay(nextDay);
            nextDay = testDay.PlusDays(1);

            var result = (testDay + time)
                .InZoneLeniently(tz)
                .ToInstant();

            if (result >= clock.GetCurrentInstant())
                yield return result;
        }
    }

    public RotationService(DiscordSocketClient discord, TimezoneProviderService timezoneProvider, PinService pins, ILogger<RotationService> logger)
    {
        this.pins = pins;
        this.logger = logger;
        this.discord = discord;
        this.timezoneProvider = timezoneProvider;

        discord.GuildScheduledEventCompleted += Discord_GuildScheduledEventCompleted;
    }

    private async Task Discord_GuildScheduledEventCompleted(SocketGuildEvent arg)
    {
        var instance = Instance.Get(arg.Guild.Id);
        var trackedEvent = instance.Database
            .Select<TrackedEvent>()
            .FirstOrDefault(x => x.DiscordEventId == arg.Id);

        if (trackedEvent == null)
            return;

        using var rotation = new RotationList(instance);
        if (!rotation.AdvanceRotation())
            return;

        using (var s = instance.Database.BeginSession())
            s.Delete(trackedEvent);

        await CreateEventForNextRotation(instance, rotation);
        await PostRotationMessage(instance, rotation);
    }


    private readonly DiscordSocketClient discord;
    private readonly TimezoneProviderService timezoneProvider;
    private readonly PinService pins;
    private readonly ILogger<RotationService> logger;
}
