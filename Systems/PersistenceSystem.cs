using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems
{
    public class PersistenceSystem
    {
        public PersistenceSystem(Orchestrator orchestrator)
        {
            orchestrator.OnTick += Orchestrator_OnTick;
        }

        private Task Orchestrator_OnTick(object? sender, EventArgs e)
        {
            return Task.Run(() =>
            {
                Instance.PersistAll();
            });
        }
    }
}
