using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility.Persistence
{
    public interface IPersistable
    {
        public void Persist(string toDirectory);
        public void LoadPersistentData(string fromDirectory);
    }
}
