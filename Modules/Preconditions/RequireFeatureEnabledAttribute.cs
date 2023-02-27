using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Models;

namespace Jekbot.Modules.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireFeatureEnabledAttribute : PreconditionAttribute
    {
        /// <summary>
        /// A command or group with this attribute will require Jekbot to have a role called `Jekbot.{Name}` in order to run
        /// </summary>
        /// <param name="name"></param>
        public RequireFeatureEnabledAttribute(FeatureId id)
        {
            featureId = id;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                var instance = Instance.Get(context.Guild.Id);
                if (instance.IsFeatureEnabled(featureId))
                    return PreconditionResult.FromSuccess();
                else
                    return PreconditionResult.FromError($"The {featureId} feature is not enabled.");
            }
            else
                return PreconditionResult.FromError("You must be in a guild to run this command.");
        }

        private readonly FeatureId featureId;
    }
}
