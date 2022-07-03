using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Jekbot.Modules.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireFeatureEnabledAttribute : PreconditionAttribute
    {
        /// <summary>
        /// A command or group with this attribute will require Jekbot to have a role called `Jekbot.{Name}` in order to run
        /// </summary>
        /// <param name="name"></param>
        public RequireFeatureEnabledAttribute(string name)
        {
            _name = $"Jekbot.{name}";
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (await context.Guild.GetCurrentUserAsync().ConfigureAwait(false) is SocketGuildUser botUser)
                {
                    if (botUser.Roles.Any(r => r.Name == _name))
                        return PreconditionResult.FromSuccess();
                    else
                        return PreconditionResult.FromError($"Feature not enabled; Jekbot is missing the`{_name}` role.");
                }
                else
                    return PreconditionResult.FromError("Couldn't get the bot user");
            }
            else
                return PreconditionResult.FromError("You must be in a guild to run this command.");
        }

        private readonly string _name;
    }
}
