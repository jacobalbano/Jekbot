using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules.Preconditions;
using Jekbot.Systems;
using Jekbot.TypeConverters;
using Microsoft.Extensions.Logging;
using NodaTime;
using System.Text;
using System.Text.Json.Serialization;

namespace Jekbot.Modules;

public class RotationModule : InteractionModuleBase<SocketInteractionContext>
{
    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);
        discord.GuildScheduledEventCompleted += Discord_GuildScheduledEventCompleted;
    }

    private class RotationList : List<RotationEntry>, IDisposable
    {
        public RotationList(Instance instance)
        {
            this.instance = instance;
            AddRange(instance.Database
                .Select<RotationEntry>()
                .OrderBy(x => x.Order)
                .ToEnumerable());
        }

        public void Dispose()
        {
            instance.Database.DeleteAll<RotationEntry>();
            for (int i = 0; i < Count; i++)
                instance.Database.Insert(this[i] with { Order = i });
        }

        private readonly Instance instance;
    }

    private async Task Discord_GuildScheduledEventCompleted(SocketGuildEvent arg)
    {
        var instance = Instance.Get(arg.Guild.Id);
        var trackedEvent = instance.Database
            .Select<TrackedEvent>()
            .Where(x => x.DiscordEventId == arg.Id)
            .FirstOrDefault();

        if (trackedEvent == null) return;

        using var rotation = new RotationList(instance);
        if (!RotationSystem.AdvanceRotation(rotation))
            return;

        await rotationSystem.CreateEventForNextRotation(instance, rotation);
        await rotationSystem.PostRotationMessage(instance, rotation);
    }

    public RotationModule(DiscordSocketClient discord, ILogger<RotationModule> logger, RotationSystem rotationSystem)
    {
        this.discord = discord;
        this.logger = logger;
        this.rotationSystem = rotationSystem;
        discord.GuildScheduledEventCompleted += Discord_GuildScheduledEventCompleted;
    }

    private readonly DiscordSocketClient discord;
    private readonly ILogger<RotationModule> logger;
    private readonly RotationSystem rotationSystem;

    public record class Config : ModelBase
    {
        public ulong? ChannelId { get; set; }
        public DayOfWeek? ScheduledDay { get; set; }
        public string? ScheduledTime { get; set; }
        public string? SchedulingRelativeToTz { get; set; }

        internal bool IsConfigured()
        {
            return !(SchedulingRelativeToTz == null || ScheduledTime == null || ScheduledDay == null);
        }
    }

    [RequireContext(ContextType.Guild)]
    [Group("rotation", "Commands for Game Night")]
    [RequireFeatureEnabled(FeatureId.GameNight)]
    public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("add", "Add a user to the rotation list")]
        public async Task Add(IUser user, IUser? before = null)
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using var rotation = new RotationList(instance);

            if (rotation.Any(x => x.DiscordUserId == user.Id))
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription("User is already in the rotation!")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }

            int index = 0;
            if (before == null)
            {
                rotation.Add(new RotationEntry { DiscordUserId = user.Id });
                index = rotation.Count - 1;
            }
            else
            {
                index = rotation.FindIndex(x => x.DiscordUserId == before.Id);
                if (index < 0)
                {
                    await FollowupAsync(embed: new EmbedBuilder()
                        .WithDescription("'Before' user is not in the rotation!")
                        .WithColor(Color.Red)
                        .Build());
                    return;
                }

                rotation.Insert(index, new RotationEntry { DiscordUserId = user.Id });
            }

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"{user.Mention} has been added to the rotation at position #{index + 1}")
                .Build());
            await rotationSystem.RefreshEvents(instance, rotation);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

        [SlashCommand("remove", "Remove a user from the rotation list")]
        public async Task Remove(IUser user)
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using var rotation = new RotationList(instance);

            var rot = rotation.FirstOrDefault(x => x.DiscordUserId == user.Id);
            if (rot == null)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription("User is not in the rotation!")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }

            rotation.Remove(rot);

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"{user.Mention} has been removed from the rotation")
                .Build());
            await rotationSystem.RefreshEvents(instance, rotation);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

        [SlashCommand("swap", "Swap two users' positions in the rotation")]
        public async Task Swap(IUser first, IUser second)
        {
            await DeferAsync();

            if (first.Id == second.Id)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription("Users must be different!")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }

            var instance = Context.GetInstance();
            using var rotation = new RotationList(instance);

            int firstPos = -1, secondPos = -1;
            for (int i = 0; i < rotation.Count; i++)
            {
                if (rotation[i].DiscordUserId == first.Id)
                    firstPos = i;
                else if (rotation[i].DiscordUserId == second.Id)
                    secondPos = i;
            }

            if (firstPos < 0)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription($"{first.Mention} is not in the rotation!")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }

            if (secondPos < 0)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription($"{second.Mention} is not in the rotation!")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }

            (rotation[secondPos], rotation[firstPos]) = (rotation[firstPos], rotation[secondPos]);

            var sb = new StringBuilder();
            sb.AppendLine($"{first.Mention} is now in position #{secondPos + 1}");
            sb.AppendLine($"{second.Mention} is now in position #{firstPos + 1}");

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription(sb.ToString())
                .Build());

            await rotationSystem.RefreshEvents(instance, rotation);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

        [SlashCommand("skip", "Skip a user's next turn in the rotation")]
        public async Task Skip(IUser? user = null)
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using var rotation = new RotationList(instance);

            user ??= Context.User;
            var pos = rotation.FindIndex(x => x.DiscordUserId == user.Id);
            if (pos < 0)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithDescription($"{user.Mention} is not in the rotation!")
                    .WithColor(Color.Red)
                    .Build());

                return;
            }

            rotation[pos] = rotation[pos] with { Skip = true };

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"Skipping {user.Mention}'s next turn")
                .Build());

            await rotationSystem.RefreshEvents(instance, rotation);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

        [SlashCommand("set-channel", "Set the channel to post rotation in")]
        public async Task SetChannel(ITextChannel channel)
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using (var config = instance.Database.GetSingleton<Config>())
                config.Value.ChannelId = channel.Id;

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"Rotation updates will now be posted to {channel.Mention}")
                .Build());

            using var rotation = new RotationList(instance);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

        [SlashCommand("set-schedule", "Set schedule")]
        public async Task SetTime(
            [Summary(description: "The time of day, e.g. 8:00pm")] string time,
            [Summary(description: "The day to repeat this event on")] DayOfWeek day,
            [Summary(description: "Your current timezone"), Autocomplete(typeof(TimezoneAutoComplete))] string timezone
        )
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using (var config = instance.Database.GetSingleton<Config>())
            {
                config.Value.ScheduledTime = time;
                config.Value.ScheduledDay = day;
                config.Value.SchedulingRelativeToTz = timezone;
            }

            var next = rotationSystem.GenerateFutureGameNightInstants(instance).First();
            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription($"Scheduling updated: next game night will be <t:{next.ToUnixTimeSeconds()}>")
                .Build());

            using var rotation = new RotationList(instance);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

        [SlashCommand("postpone", "Postpone game night one week")]
        public async Task Postpone()
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using var rotation = new RotationList(instance);

            if (rotation.FirstOrDefault() is RotationEntry next && next.Type == RotationEntryType.Postponment )
                return;

            rotation.Insert(0, new RotationEntry { Type = RotationEntryType.Postponment });

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription("Postponed one week")
                .Build());
            await rotationSystem.RefreshEvents(instance, rotation);
            await rotationSystem.PostRotationMessage(instance, rotation);
        }

#if DEBUG
        [RequireOwner]
        [SlashCommand("advance", "Advance game night manually")]
        public async Task DebugAdvance()
        {
            await DeferAsync();

            var instance = Context.GetInstance();
            using var rotation = new RotationList(instance);
            if (!RotationSystem.AdvanceRotation(rotation))
            {
                await FollowupAsync("Failed to advance");
                return;
            }

            await rotationSystem.RefreshEvents(instance, rotation);
            await rotationSystem.PostRotationMessage(instance, rotation);
            await FollowupAsync("Advanced");
        }
#endif

        public ConfigModule(RotationSystem rotation)
        {
            rotationSystem = rotation;
        }

        private readonly RotationSystem rotationSystem;
    }
}