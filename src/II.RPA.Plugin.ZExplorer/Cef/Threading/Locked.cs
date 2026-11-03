using System;
using System.Threading;

namespace II.RPA.Plugin.ZExplorer.Cef.Threading
{
    public static class Locked
    {
        public static Locked<T> From<T>(T value)
        {
            return new Locked<T>(value);
        }
    }
    public sealed class Locked<T>
    {
        public Locked() : this(default(T))
        {
        }

        public Locked(T value)
        {
            this.mutex = new object();
            this.value = value;
        }

        public IWriteScope<T> Access()
        {
            return new Locked<T>.WriteScope(this);
        }

        private readonly object mutex;

        private T value;

        private sealed class WriteScope : IWriteScope<T>, IReadScope<T>, IDisposable
        {
            internal WriteScope(Locked<T> outer)
            {
                this.outer = outer;
                Monitor.Enter(this.outer.mutex);
            }

            public T Value
            {
                get
                {
                    return this.outer.value;
                }
                set
                {
                    this.outer.value = value;
                }
            }

            public void Dispose()
            {
                Monitor.Exit(this.outer.mutex);
            }

            private readonly Locked<T> outer;
        }
    }
}
