using Discord.WebSocket;
using Jekbot.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
    public class ActionTimerSystem
    {
        public ActionTimerSystem(ConfigFile config, DiscordSocketClient client)
        {
            this.config = config;
            this.client = client;
        }

        public Task Start()
        {
            _ = new Timer(
                OnTick,
                null,
                config.TickMilliseconds,
                config.TickMilliseconds
            );

            return Task.CompletedTask;
        }

        private void OnTick(object? state)
        {
            using (var dbApi = Database.Prepare())
            {
                foreach (var timer in dbApi.GetPassedTimers())
                {
                    Console.WriteLine($"Timer #{timer.Id} passed");
                }
            }
        }

        private readonly ConfigFile config;
        private readonly DiscordSocketClient client;
    }
}
