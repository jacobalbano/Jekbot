using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public interface IFactory<TResource>
    {
        TResource Create();
    }

    public abstract class PreparableResource<TSelf, TFactory>
        where TSelf : PreparableResource<TSelf, TFactory>
        where TFactory : IFactory<TSelf>, new()
    {
        public static TSelf Prepare()
        {
            return factory.Create();
        }

        private static readonly TFactory factory = new();
    }
}
