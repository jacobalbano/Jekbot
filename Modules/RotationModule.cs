using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules.Preconditions;
using Jekbot.Services;
using Jekbot.TypeConverters;
using Jekbot.Utility;
using System.Text;

namespace Jekbot.Modules;

[RequireContext(ContextType.Guild)]
[Group("rotation", "Commands for Game Night")]
[RequireFeatureEnabled(FeatureId.GameNight)]
public class RotationModule : InteractionModuleBase<SocketInteractionContext>
{
    public RotationModule(RotationService rotationService)
    {
        this.rotationService = rotationService;
    }

    public record class Config : ModelBase
    {
        public ulong? ChannelId { get; set; }
        public DayOfWeek? ScheduledDay { get; set; }
        public string? ScheduledTime { get; set; }
        public string? SchedulingRelativeToTz { get; set; }

        public bool IsConfigured()
        {
            return SchedulingRelativeToTz != null && ScheduledTime != null && ScheduledDay != null && ChannelId != null;
        }
    }

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
        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
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
        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
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

        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
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

        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
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
        await rotationService.PostRotationMessage(instance, rotation);
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

        var next = rotationService.GenerateFutureGameNightInstants(instance).First();
        await FollowupAsync(embed: new EmbedBuilder()
            .WithDescription($"Scheduling updated: next game night will be <t:{next.ToUnixTimeSeconds()}>")
            .Build());

        using var rotation = new RotationList(instance);
        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
    }

    [SlashCommand("postpone", "Postpone game night one week")]
    public async Task Postpone()
    {
        await DeferAsync();

        var instance = Context.GetInstance();
        using var rotation = new RotationList(instance);

        if (rotation.FirstOrDefault() is RotationEntry next && next.Type == RotationEntryType.Postponment)
            return;

        rotation.Insert(0, new RotationEntry { Type = RotationEntryType.Postponment });

        await FollowupAsync(embed: new EmbedBuilder()
            .WithDescription("Postponed one week")
            .Build());
        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
    }

//#if DEBUG
    [RequireOwner]
    [SlashCommand("advance", "Advance game night manually")]
    public async Task DebugAdvance()
    {
        await DeferAsync();

        var instance = Context.GetInstance();
        using var rotation = new RotationList(instance);
        if (!rotation.AdvanceRotation())
        {
            await FollowupAsync("Failed to advance");
            return;
        }

        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
        await FollowupAsync("Rotation advanced");
    }
//#endif

    [RequireOwner]
    [SlashCommand("refresh", "Refresh rotation")]
    public async Task RefreshEvents()
    {
        await DeferAsync();

        var instance = Context.GetInstance();
        using var rotation = new RotationList(instance);
        await rotationService.RefreshEvents(instance, rotation);
        await rotationService.PostRotationMessage(instance, rotation);
        await FollowupAsync("Rotation refreshed");
    }

    private readonly RotationService rotationService;
}