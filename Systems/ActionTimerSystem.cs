using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules;
using Jekbot.Utility;
using Jekbot.Utility.Persistence;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems;

public class ActionTimerSystem
{
    public ActionTimerSystem(DiscordSocketClient discord, Orchestrator orchestrator, RotationSystem rotation)
    {
        orchestrator.OnTick += Orchestrator_OnTick;
        this.discord = discord;
        this.rotation = rotation;
    }

    private async Task Orchestrator_OnTick(object? sender, EventArgs e)
    {
        foreach (var guild in discord.Guilds)
            await OnTickInterval(Instance.Get(guild.Id));
    }

    public async Task OnTickInterval(Instance instance)
    {
        foreach (var timer in GetPassedTimers(instance))
        {
            switch (timer.Type)
            {
                case ActionTimerType.Rotation:
                    await rotation.HandleRotationTimer(instance, timer);
                    break;
                case ActionTimerType.RotationDayAfter:
                    await rotation.HandleRotationDayAfterTimer(instance, timer);
                    break;
                case ActionTimerType.None:
                default:
                    break;
            }
        }
    }

    public void ClearTimers(Instance instance, ActionTimerType type)
    {
        for (int i = instance.Timers.Count; i-- > 0;)
        {
            if (instance.Timers[i].Type == type)
                instance.Timers.RemoveAt(i);
        }
    }

    private static IEnumerable<ActionTimer> GetPassedTimers(Instance instance)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var timers = instance.Timers;
        for (int i = timers.Count; i-- > 0;)
        {
            var x = timers[i];
            if (x.ExpirationInstant <= now)
            {
                yield return x;
                timers.RemoveAt(i);
            }
        }
    }

    private readonly DiscordSocketClient discord;
    private readonly RotationSystem rotation;
}
