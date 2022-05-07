using Discord;
using Discord.WebSocket;

namespace Jekbot;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        var config = ConfigFile.LoadOrCreate();

        var client = new DiscordSocketClient();
        client.Log += Log;
        await client.LoginAsync(TokenType.Bot, config.Token);
        await client.StartAsync();
        await Task.Delay(-1);
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
