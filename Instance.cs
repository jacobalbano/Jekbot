using Discord.Interactions;
using Jekbot.Models;

namespace Jekbot
{
    public class Instance
    {
        public ulong Id { get; init; }
        public Database Database { get; }

        public static Instance Get(ulong id)
        {
            if (Instances.TryGetValue(id, out var instance))
                return instance;

            return Establish(id);
        }

        public bool IsFeatureEnabled(FeatureId featureId)
        {
            return features.TryGetValue(featureId, out var result) && result;
        }

        public void SetFeatureEnabled(FeatureId featureId, bool enabled)
        {
            features[featureId] = enabled;
            using var s = Database.BeginSession();
            s.InsertOrUpdate((Database.Select<BotFeature>()
                .Where(x => x.Feature == featureId)
                .SingleOrDefault() ?? new() { Feature = featureId }) with { Enabled = enabled });
        }

        #region implementation
        private Instance(ulong id)
        {
            Id = id;
            Database = new(id.ToString(), "Data");
            foreach (var item in Database.Select<BotFeature>().ToEnumerable())
                features[item.Feature] = item.Enabled;
        }

        private static Instance Establish(ulong id)
        {
            if (!Directory.Exists(id.ToString()))
                Directory.CreateDirectory(id.ToString());

            return Instances[id] = new Instance(id);
        }

        private static readonly Dictionary<ulong, Instance> Instances = new();
        private readonly Dictionary<FeatureId, bool> features = new();
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
