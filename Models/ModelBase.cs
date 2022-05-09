using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Models
{
    public abstract class ModelBase
    {
        public ulong Id { get; set; }
        public ulong GuildId { get; set; }
    }
}
