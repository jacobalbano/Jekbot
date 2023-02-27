using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules;
using Jekbot.Utility;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
    [AutoDiscoverSingletonService]
    public class RotationSystem
    {
        public async Task RefreshEvents(Instance instance, List<RotationEntry> rotation)
        {
            /*
             * Advances past any immediately-skipped users
             * Creates an event for the current rotation if one does not exist
             * Cancels any other events
             */

            if (rotation.Count == 0)
                return;

            AdvancePastSkippedUsers(rotation);

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
                        .Where(x => x.Key == key)
                        .FirstOrDefault();

                    if (trackedEvent != null)
                    {
                        try
                        {
                            var discordEvent = await guild.GetEventAsync(trackedEvent.DiscordEventId);
                            if (discordEvent != null)
                                await discordEvent.DeleteAsync();

                            instance.Database.Delete(trackedEvent);
                        }
                        catch (Exception)
                        {
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
                channelId: guild.VoiceChannels.FirstOrDefault()?.Id
            );

            var newEvent = new TrackedEvent { DiscordEventId = guildEvent.Id };
            instance.Database.Insert(newEvent);
            return newEvent;
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
                        .Where(x => x.Key == next.TrackedEventKey)
                        .FirstOrDefault();

                    if (trackedEvent != null)
                    {
                        sb.AppendLine("Please RSVP below to help us pick what to play next time!");
                        sb.AppendLine(GuildEventUtility.CreateEventLink(instance.Id, trackedEvent.DiscordEventId));
                    }
                }

                var message = await channel.SendMessageAsync(sb.ToString());

                try
                {
                    await pins.PinMessage(instance, channel.Id, message.Id, "Rotation");
                }
                catch (Exception)
                {
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

        public bool AdvanceRotation(Instance instance, List<RotationEntry> rotation)
        {
            if (rotation.Count == 0)
                return false;
            
            AdvancePastSkippedUsers(rotation);
            if (rotation[0].Type == RotationEntryType.User)
                rotation.Add(rotation[0]);

            rotation.RemoveAt(0);
            AdvancePastSkippedUsers(rotation);
            return true;
        }

        private static void AdvancePastSkippedUsers(List<RotationEntry> rotation)
        {
            while (rotation[0].Skip)
            {
                var move = rotation[0];
                rotation.RemoveAt(0);
                rotation.Add(move with { Skip = false });
            }
        }

        public RotationSystem(DiscordSocketClient discord, TimezoneProvider timezoneProvider, PinSystem pins)
        {
            this.discord = discord;
            this.timezoneProvider = timezoneProvider;
            this.pins = pins;
        }

        private readonly DiscordSocketClient discord;
        private readonly TimezoneProvider timezoneProvider;
        private readonly PinSystem pins;
    }
}
