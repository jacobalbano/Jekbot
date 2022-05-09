using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public class Disposable
    {
        public interface Contract : IDisposable
        {
            public void DisposeOnce();
            public bool Disposed { get; set; }

            void IDisposable.Dispose()
            {
                if (!Disposed)
                {
                    DisposeOnce();
                
                    GC.SuppressFinalize(this);
                    Disposed = true;
                }
            }
        }

        public static void Ensure(Contract disposable)
        {
            if (disposable.Disposed)
                throw new ObjectDisposedException(disposable.GetType().Name);
        }
    }
}
