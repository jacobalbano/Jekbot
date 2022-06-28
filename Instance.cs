using Discord.Interactions;
using Jekbot.Models;
using Jekbot.Modules;
using Jekbot.Systems;
using Jekbot.Utility;
using Jekbot.Utility.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot
{
    public class Instance : IPersistable
    {
        public ulong Id { get; init; }

        public static ConfigFile BotConfig { get; } = ConfigFile.Prepare();

        public PersistableList<ActionTimer> Timers { get; } = new("Timers");
        public PersistableList<RotationEntry> Rotation { get; } = new("Rotation");
        public PersistableList<TrackedEvent> GuildEvents { get; } = new("GuildEvents");
        public PersistableList<PinnedMessage> PinnedMessages { get; } = new("PinnedMessages");

        public RotationSystem.Config RotationConfig { get; } = new();

        public static Instance Get(ulong id)
        {
            if (Instances.TryGetValue(id, out var instance))
                return instance;

            return Establish(id);
        }

        public static void PersistAll()
        {
            foreach (var (id, instance) in Instances)
                instance.Persist(id.ToString());
        }

        #region implementation
        private Instance() { }

        private static Instance Establish(ulong id)
        {
            if (Directory.Exists(id.ToString()))
                return Load(id);

            Directory.CreateDirectory(id.ToString());
            return Instances[id] = new Instance { Id = id };
        }
        
        private static Instance Load(ulong id)
        {
            var result = new Instance { Id = id };
            result.LoadPersistentData(id.ToString());
            Instances.Add(id, result);
            return result;
        }

        public void Persist(string toDirectory)
        {
            foreach (var getChild in Persistables)
                getChild(this).Persist(toDirectory);
        }

        public void LoadPersistentData(string fromDirectory)
        {
            foreach (var getChild in Persistables)
                getChild(this).LoadPersistentData(fromDirectory);
        }

        private static readonly Dictionary<ulong, Instance> Instances = new();
        private static readonly IReadOnlyList<GetPersistable> Persistables = DiscoverPersistables();
        private static IReadOnlyList<GetPersistable> DiscoverPersistables()
        {
            return typeof(Instance)
                .GetProperties()
                .Where(x => typeof(IPersistable).IsAssignableFrom(x.PropertyType))
                .Select(x =>
                {
                    var param = Expression.Parameter(typeof(Instance));
                    return Expression.Lambda<GetPersistable>(
                        Expression.TypeAs(Expression.Property(param, x), typeof(IPersistable)),
                        param
                    ).Compile();
                }).ToList();
        }

        private delegate IPersistable GetPersistable(Instance instance);
        #endregion
    }

    public static class InstanceExt
    {
        public static Instance GetInstance(this SocketInteractionContext context)
        {
            return Instance.Get(context.Guild.Id);
        }
    }
}
