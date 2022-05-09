using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Models
{
    [Flags]
    public enum ActionTimerType : byte
    {
        None,
        ScheduledEventWarning,
    }

    public class ActionTimer : ModelBase
    {
        public DateTime ExpirationUtc { get; set; }
        public ActionTimerType Type { get; set; }
        public bool Processed { get; set; }
    }
}
