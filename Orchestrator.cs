using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
    public class Orchestrator
    {
        public event AsyncEventHandler OnTick;

        public Task Start()
        {
            SetNextTick();
            return Task.CompletedTask;
        }

        private void OnTickInterval(object? state)
        {
            var handler = OnTick;
            if (handler != null)
            {
                Task.Run(async () =>
                {
                    await Task.WhenAll(handler.GetInvocationList()
                        .Cast<AsyncEventHandler>()
                        .Select(x => x.Invoke(this, EventArgs.Empty))
                    );

                    SetNextTick();
                });
            }
            else
            {
                SetNextTick();
            }
        }

        private void SetNextTick()
        {
            tick.Change(Instance.BotConfig.TickMilliseconds, Timeout.Infinite);
        }

        public Orchestrator()
        {
             tick = new Timer(OnTickInterval, null, Timeout.Infinite, Timeout.Infinite);
        }

        public delegate Task AsyncEventHandler(object sender, EventArgs e);
        private readonly Timer tick;
    }
}
