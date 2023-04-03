using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Models
{
    public record class HandledEvent : ModelBase
    {
        [Indexed]
        public ulong DiscordEventId { get; init; }
    }
}
