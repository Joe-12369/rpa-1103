using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    /// <summary>
    /// 一个相对健壮的生产-消费模式的实现类、但有些过时，可以被BlockCollection取代
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TaskQueue<T> : IDisposable where T : class
    {
        object locker = new object();
        Action<T> m_Action;
        Thread[] workers;
        Queue<T> taskQ = new Queue<T>();

        public TaskQueue(int workerCount,Action<T> anAction)
        {
            m_Action = anAction;
            workers = new Thread[workerCount];
            // Create and start a separate thread for each worker
            for (int i = 0; i < workerCount; i++)
            {
                (workers[i] = new Thread(Consume)).Start();
            }
        }

        public void Dispose()
        {
            // Enqueue one null task per worker to make each exit.
            foreach (Thread worker in workers) EnqueueTask(null);
            foreach (Thread worker in workers) worker.Join();
        }

        public void EnqueueTask(T task)
        {
            lock (locker)
            {
                taskQ.Enqueue(task);
                Monitor.PulseAll(locker);
            }
        }

        void Consume()
        {
            while (true)
            {
                T task;
                lock (locker)
                {
                    while (taskQ.Count == 0) Monitor.Wait(locker);
                    task = taskQ.Dequeue();
                }
                if (task == null) return;
                m_Action(task);
            }
        }
    }
}
