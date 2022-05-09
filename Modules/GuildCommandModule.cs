using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Modules;

[Group("rotation", "Commands for Game Night")]
public class GuildCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("add", "Add a user to the rotation list")]
    public async Task Add(IUser user)
    {
        await RespondAsync($"{user.Username} has been added to the rotation (not really)");
    }

    [SlashCommand("swap", "Swap two users' positions in the rotation")]
    public async Task Swap(IUser first, IUser second)
    {
        await RespondAsync($"Swapping positions for {first.Username} and {second.Username} in the rotation (not really)");
    }

    [SlashCommand("skip", "Skip a user's next turn in the rotation")]
    public async Task Skip(IUser? user = null)
    {
        user ??= Context.User;
        await RespondAsync($"Skipping {user.Username}'s next turn (not really)");
    }

    [SlashCommand("set-channel", "Set the channel to post rotation in")]
    public async Task SetChannel(IChannel channel)
    {
        await RespondAsync($"Rotation updates will now be posted to <#{channel.Id}> (not really)");
    }
}