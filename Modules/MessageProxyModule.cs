using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Jekbot.Modules
{
    public class MessageProxyModule : InteractionModuleBase<SocketInteractionContext>
    {
        public override void Construct(ModuleBuilder builder, InteractionService commandService)
        {
            base.Construct(builder, commandService);
            discord.MessageReceived += Discord_MessageReceived;
        }

        private async Task Discord_MessageReceived(SocketMessage message)
        {
            //  only pay attention to DMs
            if (message.Channel is not SocketDMChannel)
                return;

            //  don't respond to messages from bots (including ourselves)
            if (message.Author.IsBot)
                return;

            var owner = await Owner.Value;
            IUser userToSend = owner;
            if (message.Author.Id == owner.Id)
            {
                if (message is SocketUserMessage msg && msg.ReferencedMessage != null)
                {
                    var refEmbed = msg.ReferencedMessage.Embeds
                        .FirstOrDefault(x => x.Fields.Any(x => x.Name == "secret" && x.Value == currentSecret))
                        ?? throw new Exception("Couldn't find secure embed in relayed message");

                    var possibleUserId = refEmbed.Fields
                        .FirstOrDefault(x => x.Name == "userid")
                        .Value;

                    if (!ulong.TryParse(possibleUserId, out var userId))
                        throw new Exception("Couldn't find user for relayed message");

                    userToSend = await discord.GetUserAsync(userId);
                }
                else
                {
                    //  only respond to messages from owner if it's a reply
                    return;
                }
            }

            //  prepare the embed for sending
            var embed = new EmbedBuilder()
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .WithCurrentTimestamp();

            //  only send fields if we're sending *to* the owner
            //  for security (see below) and also 
            if (userToSend.Id == owner.Id)
            {
                embed.AddField("userid", message.Author.Id, true);
                embed.AddField("secret", currentSecret);
            }

            try
            {
                await userToSend.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send message to user");
            }
        }

        public MessageProxyModule(DiscordSocketClient discord, ILogger<MessageProxyModule> logger)
        {
            this.discord = discord;
            this.logger = logger;
            Owner  = new Lazy<Task<IUser>>(async() =>
            {
                var info = await discord.GetApplicationInfoAsync();
                return info.Owner;
            });
        }

        private readonly DiscordSocketClient discord;
        private readonly Lazy<Task<IUser>> Owner;
        private readonly ILogger<MessageProxyModule> logger;

        //  rudimentary security to make sure we find the right embed
        //  secret will change every time the bot restarts
        private readonly string currentSecret = Guid.NewGuid().ToString();
    }
}
