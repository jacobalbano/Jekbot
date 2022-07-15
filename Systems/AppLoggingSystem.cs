using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Jekbot.Utility;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
    [AutoDiscoverSingletonService, ForceInitialization]
    public class AppLoggingSystem
    {
        public AppLoggingSystem(DiscordSocketClient client, InteractionService commands, ILogger<AppLoggingSystem> logger)
        {
            client.Log += (msg) =>
            {
                logger.Log(GetLogLevel(msg), $"[Client] {msg.ToString()}");
                return Task.CompletedTask;
            };

            commands.Log += (msg) =>
            {
                logger.Log(GetLogLevel(msg), $"[Commands] {msg.ToString()}");
                return Task.CompletedTask;
            };
        }

        private static LogLevel GetLogLevel(LogMessage msg)
        {
            return msg.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };
        }
    }
}
