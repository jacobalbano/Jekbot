using Discord.WebSocket;
using Jekbot.Models;
using Jekbot.Modules;
using Jekbot.Utility;
using Jekbot.Utility.Persistence;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Systems;

[AutoDiscoverSingletonService, ForceInitialization]
public class ActionTimerSystem
{
    public ActionTimerSystem(DiscordSocketClient discord, Orchestrator orchestrator, RotationSystem rotation, ILogger<ActionTimerSystem> logger)
    {
        orchestrator.OnTick += Orchestrator_OnTick;
        this.discord = discord;
        this.rotation = rotation;
        this.logger = logger;
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
            try
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

                logger.LogInformation($"Handled {timer.Type} timer");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error handling {timer.Type}");
                throw;
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
    private readonly ILogger<ActionTimerSystem> logger;
}
