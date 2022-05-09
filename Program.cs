using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Modules;
using Jekbot.Resources;
using Jekbot.TypeConverters;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Jekbot;

public class Program
{
    static void Main(string[] args)
    {
        RunAsync().GetAwaiter().GetResult();
    }

    static async Task RunAsync()
    {
        using var services = ConfigureServices();

        var client = services.GetRequiredService<DiscordSocketClient>();
        var commands = services.GetRequiredService<InteractionService>();
        var config = services.GetRequiredService<ConfigFile>();
        var handler = services.GetRequiredService<CommandHandler>();

        // Registering a concrete type TypeConverter
        commands.AddTypeConverter<GuildPermissions>(new GuildPermissionsTypeConverter());

        await handler.Initialize();

        await client.LoginAsync(TokenType.Bot, config.Token);

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
        await Task.Delay(Timeout.Infinite);
    }

    static ServiceProvider ConfigureServices() =>
        new ServiceCollection()
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton(ConfigFile.Prepare())
        .AddSingleton<InteractionService>()
        .AddSingleton<CommandHandler>()
        //.AddSingleton<PerfectlyRealisticDB>()
        .BuildServiceProvider();
}
