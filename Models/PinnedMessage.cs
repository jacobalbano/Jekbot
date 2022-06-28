using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Models
{
    public record class PinnedMessage : ModelBase
    {
        public ulong DiscordChannelId { get; init; }
        public ulong DiscordMessageId { get; init; }

        public string UniqueName { get; init; }
    }
}
