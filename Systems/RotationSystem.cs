﻿using Discord;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Utility.Persistence;
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
    public class RotationSystem
    {
        public class Config : PersistableConfig<Config>
        {
            public ulong? ChannelId
            {
                get => Get<ulong?>();
                set => Set(value);
            }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public DayOfWeek? ScheduledDay
            {
                get => Get<DayOfWeek?>();
                set => Set(value);
            }

            public string? ScheduledTime
            {
                get => Get<string?>();
                set => Set(value);
            }

            public string? SchedulingRelativeToTz
            {
                get => Get<string?>();
                set => Set(value);
            }
        }

        public async Task HandleRotationTimer(Instance instance, ActionTimer timer)
        {
            /*
             * == Fired when game night is about to happen ==
             * Get the next entry in rotation
             * Check if there's a corresponding TrackedEvent
             * Get the event from the server
             * Ping the users who RSVP'd to the event
             * Post a link to the event
             * Remove the TrackedEvent
             * Add a new timer for 12 hours later that will update the rotation
             */

            var rot = instance.Rotation.FirstOrDefault();
            if (rot == null)
                return;

            var trackedEvent = instance.GuildEvents
                .FirstOrDefault(x => x.Key == rot.TrackedEventKey);

            if (trackedEvent != null)
            {
                var guild = discord.GetGuild(instance.Id);
                var guildEvent = await guild.GetEventAsync(trackedEvent.DiscordEventId);
                if (guildEvent != null && instance.RotationConfig.ChannelId is ulong channelId)
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
                        sb.AppendLine(CreateEventLink(instance.Id, guildEvent.Id));

                        await channel.SendMessageAsync(sb.ToString());
                    }
                }

                instance.GuildEvents.Remove(trackedEvent);
            }

            instance.Timers.Add(new ActionTimer {
                Type = ActionTimerType.RotationDayAfter,
                ExpirationInstant = timer.ExpirationInstant.Plus(Duration.FromHours(12))
            });
        }

        public async Task HandleRotationDayAfterTimer(Instance instance, ActionTimer timer)
        {
            /*
             * == Fired the day after game night to set things up for next week ==
             * Advance the rotation up to the earliest non-skipped entry
             * Create a new guild event
             * Register the event in TrackedEvents
             * Add a new timer for next week at five minutes before game night starts
             * Post the updated rotation in the channel with a link to the new event
             */

            var guild = discord.GetGuild(instance.Id);
            var rotation = instance.Rotation;

            if (!AdvanceRotation(rotation, out var next))
                return;

            var user = await discord.GetUserAsync(next.DiscordUserId);
            var nextGameNight = GenerateFutureGameNightInstants(instance).First();
            var guildEvent = await guild.CreateEventAsync(
                $"Game Night - {user.Username}",
                nextGameNight.ToDateTimeUtc(),
                GuildScheduledEventType.Voice,
                channelId: guild.VoiceChannels.FirstOrDefault()?.Id
            );

            var newEvent = new TrackedEvent { DiscordEventId = guildEvent.Id };
            instance.GuildEvents.Add(newEvent);

            rotation[0] = next with { TrackedEventKey = newEvent.Key };

            await PostRotationMessage(instance);
        }

        public async Task PostRotationMessage(Instance instance)
        {
            /*
             * Generate a list of dates for each upcoming game night
             * Send a message to the updates channel with a date assigned to each non-skipped user
             * Post a link to the upcoming game night event
             */

            if (instance.Rotation.Count == 0)
                return;

            var guild = discord.GetGuild(instance.Id);
            if (instance.RotationConfig.ChannelId is ulong channelId && guild.GetChannel(channelId) is ITextChannel channel)
            {
                using var intervals = GenerateFutureGameNightInstants(instance)
                    .GetEnumerator();

                var sb = new StringBuilder();
                sb.AppendLine("Current rotation:");
                sb.Append(":star: ");
                foreach (var item in instance.Rotation)
                {
                    var user = await discord.GetUserAsync(item.DiscordUserId);
                    sb.Append($"{user.Mention} ");
                    if (item.Skip) sb.AppendLine("(away)");
                    else
                    {
                        intervals.MoveNext();
                        sb.AppendLine($"(<t:{intervals.Current.ToUnixTimeSeconds()}>)");
                    }
                }

                var next = instance.Rotation.FirstOrDefault() ?? throw new Exception("Rotation had no entries!");
                var trackedEvent = instance.GuildEvents.FirstOrDefault(x => x.Key == next.TrackedEventKey);
                if (trackedEvent != null)
                {
                    sb.AppendLine("Please RSVP below to help us pick what to play next time!");
                    sb.AppendLine(CreateEventLink(instance.Id, trackedEvent.DiscordEventId));
                }

                await channel.SendMessageAsync(sb.ToString());
            }
        }

        public IEnumerable<Instant> GenerateFutureGameNightInstants(Instance instance)
        {
            var cfg = instance.RotationConfig;
            if (cfg.SchedulingRelativeToTz == null || cfg.ScheduledTime == null || cfg.ScheduledDay == null)
                yield break;

            var tz = DateTimeZoneProviders.Tzdb[cfg.SchedulingRelativeToTz];
            var clock = SystemClock.Instance.InZone(tz);

            var isoDay = cfg.ScheduledDay.Value.ToIsoDayOfWeek();
            var getNextDay = DateAdjusters.NextOrSame(isoDay);
            var time = TimeOnly.Parse(cfg.ScheduledTime).ToLocalTime();

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

        private static bool AdvanceRotation(PersistableList<RotationEntry> rotation, out RotationEntry next)
        {
            next = default;
            if (rotation.Count == 0)
                return false;

            while (rotation[0].Skip)
            {
                var move = rotation[0];
                rotation.RemoveAt(0);
                rotation.Add(move with { Skip = false });
            }

            rotation.Add(rotation[0]);
            rotation.RemoveAt(0);
            next = rotation[0];
            return true;
        }

        private static string CreateEventLink(ulong guildId, ulong discordEventId)
        {
            return $"https://discord.com/events/{guildId}/{discordEventId}";
        }

        public RotationSystem(DiscordSocketClient discord)
        {
            this.discord = discord;
        }

        private readonly DiscordSocketClient discord;
    }
}