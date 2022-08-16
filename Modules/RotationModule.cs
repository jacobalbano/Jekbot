using Discord;
using Discord.Interactions;
using Jekbot.Models;
using Jekbot.Modules.Preconditions;
using Jekbot.Systems;
using Jekbot.TypeConverters;
using NodaTime;
using System.Text;

namespace Jekbot.Modules;

[RequireContext(ContextType.Guild)]
[Group("rotation", "Commands for Game Night")]
[RequireFeatureEnabled("GameNight")]
public class RotationModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("add", "Add a user to the rotation list")]
    public async Task Add(IUser user, IUser? before = null)
    {
        var instance = Context.GetInstance();
        var rotation = instance.Rotation;
        if (rotation.Any(x => x.DiscordUserId == user.Id))
        {
            await RespondAsync("User is already in the rotation!");
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
                await RespondAsync("'Before' user is not in the rotation!");
                return;
            }

            rotation.Insert(index, new RotationEntry { DiscordUserId = user.Id });
        }

        await RespondAsync($"{user.Mention} has been added to the rotation at position #{index + 1}");
        await rotationSystem.RefreshEvents(instance);
        await rotationSystem.PostRotationMessage(instance);
    }

    [SlashCommand("remove", "Remove a user from the rotation list")]
    public async Task Remove(IUser user)
    {
        var instance = Context.GetInstance();
        var rotation = instance.Rotation;

        var rot = rotation.FirstOrDefault(x => x.DiscordUserId == user.Id);
        if (rot == null)
        {
            await RespondAsync("User is not in the rotation!");
            return;
        }

        rotation.Remove(rot);
        await RespondAsync($"{user.Mention} has been removed from the rotation");
        await rotationSystem.RefreshEvents(instance);
        await rotationSystem.PostRotationMessage(instance);
    }

    [SlashCommand("swap", "Swap two users' positions in the rotation")]
    public async Task Swap(IUser first, IUser second)
    {
        var instance = Context.GetInstance();
        if (first.Id == second.Id)
        {
            await RespondAsync("Users must be different!");
            return;
        }

        var rotation = instance.Rotation;
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
            await RespondAsync($"{first.Mention} is not in the rotation!");
            return;
        }

        if (secondPos < 0)
        {
            await RespondAsync($"{second.Mention} is not in the rotation!");
            return;
        }

        (rotation[secondPos], rotation[firstPos]) = (rotation[firstPos], rotation[secondPos]);
        var sb = new StringBuilder();
        sb.AppendLine($"{first.Mention} is now in position #{secondPos + 1}");
        sb.AppendLine($"{second.Mention} is now in position #{firstPos + 1}");
        await RespondAsync(sb.ToString());

        await rotationSystem.RefreshEvents(instance);
        await rotationSystem.PostRotationMessage(instance);
    }

    [SlashCommand("skip", "Skip a user's next turn in the rotation")]
    public async Task Skip(IUser? user = null)
    {
        var instance = Context.GetInstance();
        user ??= Context.User;
        var rotation = instance.Rotation;
        var pos = rotation.FindIndex(x => x.DiscordUserId == user.Id);
        if (pos < 0)
        {
            await RespondAsync($"{user.Mention} is not in the rotation!");
            return;
        }

        rotation[pos] = rotation[pos] with { Skip = true };
        await RespondAsync($"Skipping {user.Mention}'s next turn");

        await rotationSystem.RefreshEvents(instance);
        await rotationSystem.PostRotationMessage(instance);
    }

    [SlashCommand("set-channel", "Set the channel to post rotation in")]
    public async Task SetChannel(ITextChannel channel)
    {
        var instance = Context.GetInstance();
        var config = instance.RotationConfig;
        config.ChannelId = channel.Id;
        await RespondAsync($"Rotation updates will now be posted to {channel.Mention}");
        await rotationSystem.PostRotationMessage(instance);
    }

    [SlashCommand("set-schedule", "Set schedule")]
    public async Task SetTime(
        [Summary(description: "The time of day, e.g. 8:00pm")] string time,
        [Summary(description: "The day to repeat this event on")] DayOfWeek day,
        [Summary(description: "Your current timezone"), Autocomplete(typeof(TimezoneAutoComplete))] string timezone
    )
    {
        var instance = Context.GetInstance();
        var config = instance.RotationConfig;

        config.ScheduledTime = time;
        config.ScheduledDay = day;
        config.SchedulingRelativeToTz = timezone;

        var next = rotationSystem.GenerateFutureGameNightInstants(instance).First();
        await RespondAsync($"Scheduling updated: next game night will be <t:{next.ToUnixTimeSeconds()}>");
        UpdateTimers(instance, next);

        await rotationSystem.PostRotationMessage(instance);
    }

    [SlashCommand("postpone", "Postpone game night one week")]
    public async Task Postpone()
    {
        var instance = Context.GetInstance();
        var rotation = instance.Rotation;
        if (rotation.FirstOrDefault() is RotationEntry next && next.Type == RotationEntryType.Postponment)
            return;

        rotation.Insert(0, new RotationEntry { Type = RotationEntryType.Postponment });

        await RespondAsync($"Postponed one week");
        await rotationSystem.RefreshEvents(instance);
        await rotationSystem.PostRotationMessage(instance);
    }

    [RequireOwner]
    [SlashCommand("update-timers", "Admin command")]
    public async Task UpdateTimers()
    {
        var instance = Context.GetInstance();
        var next = rotationSystem.GenerateFutureGameNightInstants(instance).First();
        UpdateTimers(instance, next);
        await RespondAsync($"Rotation timers reset");
    }

    private void UpdateTimers(Instance instance, Instant next)
    {
        actionTimerSystem.ClearTimers(instance, ActionTimerType.Rotation);
        actionTimerSystem.ClearTimers(instance, ActionTimerType.RotationDayAfter);
        instance.Timers.Add(new ActionTimer { Type = ActionTimerType.Rotation, ExpirationInstant = next - Duration.FromMinutes(5) });
    }

    public RotationModule(ActionTimerSystem actionTimers, RotationSystem rotation)
    {
        actionTimerSystem = actionTimers;
        rotationSystem = rotation;
    }

    private readonly ActionTimerSystem actionTimerSystem;
    private readonly RotationSystem rotationSystem;
}