using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace II.RPA.Observable.Scheduling
{
    public abstract class CtsControllable : IControllable
    {
        protected CancellationTokenSource Cts { get; set; }

        public abstract void Setup(CancellationTokenSource cts);

        public virtual void Start()
        {
            if (Cts != null && Cts.IsCancellationRequested == false)
            {
                return;
            }

            Cts = new CancellationTokenSource();
            Setup(Cts);
        }

        public virtual void Stop()
        {
            if (Cts == null || Cts.IsCancellationRequested)
            {
                return;
            }

            Cts.Cancel();
        }

        public virtual void Restart()
        {
            Stop();
            Start();
        }
    }
}
