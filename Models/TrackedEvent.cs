namespace Jekbot.Models
{
    public record class TrackedEvent : ModelBase
    {
        [Indexed]
        public ulong DiscordEventId { get; init; }
    }
}
