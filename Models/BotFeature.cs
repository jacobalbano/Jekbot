using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Models
{
    public enum FeatureId
    {
        EmojiAgreement,
        GameNight,
        EventNotify,
        UserPins
    }

    public record class BotFeature : ModelBase
    {
        [Indexed]
        public FeatureId Feature { get; init; }
        public bool Enabled { get; init; }
    }
}
