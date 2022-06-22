using Jekbot.TypeConverters;
using NodaTime;
using System.Text.Json.Serialization;

namespace Jekbot.Models
{
    [Flags, JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActionTimerType : byte
    {
        None,
        Rotation,
        RotationDayAfter,
    }


    public record class ActionTimer : ModelBase
    {
        [JsonConverter(typeof(NodaInstantConverter))]
        public Instant ExpirationInstant { get; init; }

        public ActionTimerType Type { get; init; }
    }
}
