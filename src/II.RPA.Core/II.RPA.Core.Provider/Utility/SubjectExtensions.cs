using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public static class SubjectExtensions
    {
        public static void Release<T>(this Subject<T> subject)
        {
            if (!subject.IsDisposed)
            {
                subject.OnCompleted();
                // 直接释放会导致所有等待结束的订阅死锁
                subject.Dispose();
            }
        }

        public static void Release<T>(this BehaviorSubject<T> subject)
        {
            if (!subject.IsDisposed)
            {
                subject.OnCompleted();
                subject.Dispose();
            }
        }
    }
}
