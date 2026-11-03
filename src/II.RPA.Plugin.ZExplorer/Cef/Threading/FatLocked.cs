using System;
using System.Threading;

namespace II.RPA.Plugin.ZExplorer.Cef.Threading
{
    public sealed class FatLocked<T>
    {
        public FatLocked() : this(default(T))
        {
        }

        public FatLocked(T value)
        {
            this.mutex = new ReaderWriterLock();
            this.value = value;
        }

        public IReadScope<T> Readable()
        {
            return new FatLocked<T>.ReadScope(this);
        }

        public IWriteScope<T> Writable()
        {
            return new FatLocked<T>.WriteScope(this);
        }

        private const int Infinite = -1;

        private readonly ReaderWriterLock mutex;

        private T value;

        private sealed class ReadScope : IReadScope<T>, IDisposable
        {
            internal ReadScope(FatLocked<T> outer)
            {
                this.outer = outer;
                this.outer.mutex.AcquireReaderLock(-1);
            }

            public T Value
            {
                get
                {
                    return this.outer.value;
                }
            }

            public void Dispose()
            {
                this.outer.mutex.ReleaseReaderLock();
            }

            private readonly FatLocked<T> outer;
        }

        private sealed class WriteScope : IWriteScope<T>, IReadScope<T>, IDisposable
        {
            internal WriteScope(FatLocked<T> outer)
            {
                this.outer = outer;
                if (this.outer.mutex.IsReaderLockHeld)
                {
                    this.cookie = new LockCookie?(this.outer.mutex.UpgradeToWriterLock(-1));
                    return;
                }
                this.outer.mutex.AcquireWriterLock(-1);
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
                if (this.cookie != null)
                {
                    LockCookie value = this.cookie.Value;
                    this.outer.mutex.DowngradeFromWriterLock(ref value);
                    return;
                }
                this.outer.mutex.ReleaseWriterLock();
            }

            private readonly FatLocked<T> outer;

            private readonly LockCookie? cookie;
        }
    }
}
