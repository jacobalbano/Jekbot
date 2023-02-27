using LiteDB;

namespace Jekbot.Models
{
    public abstract record class ModelBase
    {
        [BsonId]
        public Guid Key { get; init; } = Guid.NewGuid();
    }
}
