namespace Jekbot.Models
{
    /// <summary>
    /// Represents a user in the rotation
    /// 
    /// </summary>
    public record class RotationEntry : ModelBase
    {
        public ulong DiscordUserId { get; init; }
        public bool Skip { get; init; }

        public Guid? TrackedEventKey { get; init; }
    }
}
