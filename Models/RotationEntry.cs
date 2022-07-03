using System.Text.Json.Serialization;

namespace Jekbot.Models
{
    [Flags, JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RotationEntryType
    {
        None,
        User,
        Postponment
    }

    /// <summary>
    /// Represents a user in the rotation
    /// </summary>
    public record class RotationEntry : ModelBase
    {
        public RotationEntryType Type { get; init; } = RotationEntryType.User;

        public ulong? DiscordUserId { get; init; }
        public bool Skip { get; init; }

        public Guid? TrackedEventKey { get; init; }
    }
}
