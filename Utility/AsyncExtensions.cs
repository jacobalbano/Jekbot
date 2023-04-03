using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public static class AsyncExtensions
    {
        public static Func<Task> ToTask(this Action action)
        {
            return () =>
            {
                action();
                return Task.CompletedTask;
            };
        }
    }
}
