using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Modules;
using Jekbot.Systems;
using Jekbot.TypeConverters;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Logging;
using Jekbot.Utility;
using Jekbot.Models;

namespace Jekbot;

public class Program
{
    public static ConfigFile BotConfig { get; } = ConfigFile.Prepare();

    static void Main(string[] args)
    {
        if (args.Any())
            Directory.SetCurrentDirectory(args[0]);

        RunAsync().GetAwaiter().GetResult();
    }

    static async Task RunAsync()
    {
        using var services = ConfigureServices();
        ForceInitializationAttribute.DiscoverAndInitialize(services);

        var client = services.GetRequiredService<DiscordSocketClient>();
        var handler = services.GetRequiredService<CommandHandlerService>();

        await handler.Initialize();
        await client.LoginAsync(TokenType.Bot, BotConfig.Token);
        await client.StartAsync();

        //  load everything upfront
        foreach (var guild in client.Guilds)
            Instance.Get(guild.Id);

        await Task.Delay(Timeout.Infinite);
    }

    static ServiceProvider ConfigureServices() =>
        new ServiceCollection()
        .DiscoverTaggedSingletons()
        .DiscoverTaggedInterfaces()
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton(new DiscordSocketConfig {
            LogGatewayIntentWarnings = false,
#if !DEBUG
            LogLevel = LogSeverity.Error
#endif
        })
        .AddSingleton<InteractionService>()
        .AddLogging(x => ConfigureLogging(x))
        .BuildServiceProvider();

    private static ILoggingBuilder ConfigureLogging(ILoggingBuilder x)
    {
        return x.AddSerilog(new LoggerConfiguration()
            .WriteTo.File("logs/jekbot.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null,
                shared: true
            )
            .WriteTo.Console()
            .CreateLogger());
    }
}
