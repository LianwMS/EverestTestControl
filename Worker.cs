using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EverestTest
{
    abstract class Worker: IDisposable
    {
        private Thread _thread;
        private CancellationTokenSource _cancel;

        public Worker()
        {
            _thread = new Thread(obj => this.DoWork((CancellationToken) obj));
            _cancel = new CancellationTokenSource();
        }

        protected abstract void DoWork(CancellationToken cancel);

        public void Start()
        {
            _thread.Start(_cancel.Token);
        }

        public void Wait()
        {
            _thread.Join();
        }

        public bool Running
        {
            get
            {
                return _thread.IsAlive;
            }
        }

        public void Cancel()
        {
            _cancel.Cancel();
        }

        public void Dispose()
        {
            if (_thread.IsAlive)
            {
                _thread.Abort();
                _thread.Join();
            }
            _cancel.Dispose();
        }
    }
}
