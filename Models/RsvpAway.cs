using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Models
{
    public record class RsvpAway : ModelBase
    {
        [Indexed]
        public Guid TrackedEventKey { get; init; }

        public List<ulong> UserIds { get; init; }
    }
}
