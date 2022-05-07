using Discord;
using Discord.WebSocket;
using Jekbot.Resources;

namespace Jekbot;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        var config = ConfigFile.Prepare();
        using var db = Database.Prepare();

        var client = new DiscordSocketClient();
        client.Log += Log;

        client.ReactionAdded += Client_ReactionAdded;
        
        client.GuildScheduledEventCreated += Client_GuildScheduledEventCreated;
        client.GuildScheduledEventCancelled += Client_GuildScheduledEventCancelled;

        await client.LoginAsync(TokenType.Bot, config.Token);
        await client.StartAsync();
        await Task.Delay(-1);
    }

    private Task Client_GuildScheduledEventCancelled(SocketGuildEvent arg)
    {
        throw new NotImplementedException();
    }

    private Task Client_GuildScheduledEventCreated(SocketGuildEvent arg)
    {
        throw new NotImplementedException();
    }

    private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        throw new NotImplementedException();
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
