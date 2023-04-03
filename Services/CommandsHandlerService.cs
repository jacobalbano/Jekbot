using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Modules;

[AutoDiscoverSingletonService]
public class CommandHandlerService
{
    private readonly InteractionService commands;
    private readonly DiscordSocketClient discord;
    private readonly ILogger<CommandHandlerService> logger;
    private readonly IServiceProvider services;

    public CommandHandlerService(InteractionService commands, DiscordSocketClient discord, ILogger<CommandHandlerService> logger, IServiceProvider services)
    {
        this.commands = commands;
        this.discord = discord;
        this.logger = logger;
        this.services = services;
    }

    public async Task Initialize()
    {
        try
        {
            await commands.AddModulesAsync(Assembly.GetExecutingAssembly(), services);
            discord.InteractionCreated += InteractionCreated;
            discord.ButtonExecuted += ButtonExecuted;
            discord.Ready += Ready;
            discord.JoinedGuild += JoinedGuild;
            commands.InteractionExecuted += Commands_InteractionExecuted;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error initializing command handler");
            throw;
        }
    }

    private async Task Commands_InteractionExecuted(ICommandInfo arg1, IInteractionContext arg2, IResult arg3)
    {
        if (arg3 is PreconditionResult result)
        {
            await arg2.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription(result.ErrorReason)
                .Build());
        }
    }

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        var ctx = new SocketInteractionContext<SocketMessageComponent>(discord, arg);
        await commands.ExecuteCommandAsync(ctx, services);
    }

    private async Task Ready()
    {
        await RegisterCommandsToAllGuilds();
        discord.Ready -= Ready;
    }

    private async Task InteractionCreated(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(discord, arg);
        await commands.ExecuteCommandAsync(ctx, services);
    }

    private async Task JoinedGuild(SocketGuild guild)
    {
        Instance.Get(guild.Id);
        await RegisterCommandsToGuild(guild.Id);
    }

    private async Task RegisterCommandsToAllGuilds()
    {
        foreach (var guild in discord.Guilds)
            await RegisterCommandsToGuild(guild.Id);
    }

    private async Task RegisterCommandsToGuild(ulong guildId)
    {
        await commands.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
    }
}
