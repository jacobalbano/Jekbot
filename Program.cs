using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Modules;
using Jekbot.Systems;
using Jekbot.TypeConverters;
using Microsoft.Extensions.DependencyInjection;

namespace Jekbot;

public class Program
{
    static void Main(string[] args)
    {
        if (args.Any())
            Directory.SetCurrentDirectory(args[0]);

        RunAsync().GetAwaiter().GetResult();
    }

    static async Task RunAsync()
    {
        using var services = ConfigureServices();

        var client = services.GetRequiredService<DiscordSocketClient>();
        var commands = services.GetRequiredService<InteractionService>();
        var handler = services.GetRequiredService<CommandHandler>();
        var orchestrator = services.GetRequiredService<Orchestrator>();
        _ = services.GetRequiredService<PersistenceSystem>();

        // Registering a concrete type TypeConverter
        commands.AddTypeConverter<GuildPermissions>(new GuildPermissionsTypeConverter());

        await handler.Initialize();
        await client.LoginAsync(TokenType.Bot, Instance.BotConfig.Token);

        client.Log += (msg) =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };

        commands.Log += (msg) =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };

        await client.StartAsync();

        //  load everything upfront
        foreach (var guild in client.Guilds)
            Instance.Get(guild.Id);

        await orchestrator.Start();
        await Task.Delay(Timeout.Infinite);
    }

    static ServiceProvider ConfigureServices() =>
        new ServiceCollection()
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton<InteractionService>()
        .AddSingleton<CommandHandler>()
        .AddSingleton<Orchestrator>()
        .AddSingleton<ActionTimerSystem>()
        .AddSingleton<PersistenceSystem>()
        .AddSingleton<RotationSystem>()
        .AddSingleton<PinSystem>()
        .AddSingleton<TimezoneProvider>()
        .BuildServiceProvider();
}
