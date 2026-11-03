using II.RPA.Plugin.ZExplorer.Cef.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    public sealed class CoalescingExecutor
    {
        public CoalescingExecutor(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            this.state = Locked.From<CoalescingExecutor.State>(new CoalescingExecutor.State());
            this.backgroundDelegate = action;
        }

        public bool IsClosed
        {
            get
            {
                bool closed;
                using (IWriteScope<CoalescingExecutor.State> writeScope = this.state.Access())
                {
                    closed = writeScope.Value.Closed;
                }
                return closed;
            }
        }

        public bool TryExecute()
        {
            Task executingTask;
            using (IWriteScope<CoalescingExecutor.State> writeScope = this.state.Access())
            {
                if (writeScope.Value.Closed)
                {
                    return false;
                }
                if (writeScope.Value.ExecutingTask == null)
                {
                    writeScope.Value.ExecutingTask = Task.Factory.StartNew(this.backgroundDelegate, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).ContinueWith(new Action<Task>(this.CleanupAfterDone), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                }
                executingTask = writeScope.Value.ExecutingTask;
            }
            executingTask.Wait();
            return true;
        }

        public void Close()
        {
            Task executingTask;
            using (IWriteScope<CoalescingExecutor.State> writeScope = this.state.Access())
            {
                executingTask = writeScope.Value.ExecutingTask;
                writeScope.Value.Closed = true;
                writeScope.Value.ExecutingTask = null;
            }
            if (executingTask != null)
            {
                executingTask.Wait();
            }
        }

        private void CleanupAfterDone(Task task)
        {
            using (IWriteScope<CoalescingExecutor.State> writeScope = this.state.Access())
            {
                writeScope.Value.ExecutingTask = null;
            }
            if (task.Exception != null)
            {
                throw new AggregateException(new Exception[]
                {
                    task.Exception
                });
            }
        }

        private readonly Locked<CoalescingExecutor.State> state;

        private readonly Action backgroundDelegate;

        private sealed class State
        {
            public Task ExecutingTask;

            public bool Closed;
        }
    }
}
