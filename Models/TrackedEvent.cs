namespace Jekbot.Models
{
    public record class TrackedEvent : ModelBase
    {
        public ulong DiscordEventId { get; init; }
    }
}
